using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

using Chaos.Cryptography.Abstractions;
using Chaos.Cryptography.Abstractions.Definitions;

namespace Chaos.Cryptography;

/// <summary>
/// Provides encryption and decryption for spans using opcodes.
/// </summary>
public sealed class Crypto : ICrypto
{
    private static readonly Encoding Ascii = Encoding.ASCII;

    private const int SessionKeyLength = 9;

    // Client seed obfuscation constants
    private const byte ClientAloXor = 112;
    private const byte ClientB_Xor = 35;
    private const byte ClientAhiXor = 116;

    // Server seed obfuscation constants
    private const byte ServerAloXor = 116;
    private const byte ServerB_Xor = 36;
    private const byte ServerAhiXor = 100;

    // ClientDecrypt expects these combined (legacy protocol)
    private const ushort ClientDecryptA_Xor = 25716; // derived from server constants
    private const ushort ServerDecryptA_Xor = 29808; // derived from client constants

    private readonly byte[] _keySalts;

    public byte[] Key { get; private set; }
    public byte Seed { get; private set; }

    private ReadOnlySpan<byte> Salts => Tables.SALT_TABLE[Seed];

    public Crypto()
        : this(0, "UrkcnItnI", string.Empty) { }

    public Crypto(byte seed, string key, string? keySaltSeed = null)
    {
        Seed = seed;
        Key = Ascii.GetBytes(key);
        _keySalts = GenerateKeySalts(string.IsNullOrEmpty(keySaltSeed) ? "default" : keySaltSeed);
    }

    public Crypto(byte seed, string keySaltSeed)
    {
        Seed = seed;
        _keySalts = GenerateKeySalts(keySaltSeed);
        FillCryptoSeed(out var a, out var b);
        Key = GenerateKey(a, b);
    }

    public byte[] GenerateKey(ushort a, byte b)
    {
        var key = new byte[SessionKeyLength];
        var salts = _keySalts;
        var saltsCount = salts.Length;
        var bSquared = b * b;

        for (var i = 0; i < SessionKeyLength; i++)
        {
            var idx = (i * (SessionKeyLength * i + bSquared) + a) % saltsCount;
            key[i] = salts[idx];
        }

        return key;
    }

    public byte[] GenerateKeySalts(string seed)
    {
        // saltTable = md5(md5(seed));
        // Repeat 31 times: saltTable += md5(saltTable);
        //
        // BUT do it without O(n^2) string concatenation:
        // maintain ASCII bytes of the growing hex string,
        // hash those bytes each time, append new hex bytes.

        Span<byte> seedBytes = stackalloc byte[Ascii.GetByteCount(seed)];
        Ascii.GetBytes(seed, seedBytes);

        Span<byte> h1 = stackalloc byte[16];
        if (!MD5.TryHashData(seedBytes, h1, out _))
            throw new CryptographicException("MD5 hashing failed (h1).");

        Span<byte> hex1 = stackalloc byte[32];
        WriteHexLower(h1, hex1);

        Span<byte> h2 = stackalloc byte[16];
        if (!MD5.TryHashData(hex1, h2, out _))
            throw new CryptographicException("MD5 hashing failed (h2).");

        // Start buffer with md5(md5(seed)) as lowercase hex ASCII bytes
        var writer = new ArrayBufferWriter<byte>(32 * (1 + 31));
        Span<byte> hex2 = stackalloc byte[32];
        WriteHexLower(h2, hex2);
        writer.Write(hex2);

        // Append md5(saltTable) 31 times, where saltTable is the entire accumulated ASCII hex string
        Span<byte> hx = stackalloc byte[16];
        Span<byte> hex = stackalloc byte[32];

        for (var i = 0; i < 31; i++)
        {
            if (!MD5.TryHashData(writer.WrittenSpan, hx, out _))
                throw new CryptographicException("MD5 hashing failed (loop).");

            WriteHexLower(hx, hex);
            writer.Write(hex);
        }

        return writer.WrittenSpan.ToArray();
    }

    #region Utility

    private static void FillCryptoSeed(out ushort a, out byte b)
    {
        Span<byte> tmp = stackalloc byte[3];
        RandomNumberGenerator.Fill(tmp);

        // ensure >= 256
        a = (ushort)(256 + ((tmp[0] << 8) | tmp[1]));
        if (a == 0) a = 256;

        // ensure >= 100
        b = (byte)(100 + (tmp[2] % (byte.MaxValue - 100)));
        if (b == 0) b = 100;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteHexLower(ReadOnlySpan<byte> src, Span<byte> dest)
    {
        const string lut = "0123456789abcdef";
        if (dest.Length < src.Length * 2)
            throw new ArgumentException("Destination too small.", nameof(dest));

        var di = 0;
        for (var i = 0; i < src.Length; i++)
        {
            var b = src[i];
            dest[di++] = (byte)lut[b >> 4];
            dest[di++] = (byte)lut[b & 0xF];
        }
    }

    #endregion

    #region Core Cipher

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyXorCipher(
        Span<byte> buffer,
        ReadOnlySpan<byte> salts,
        ReadOnlySpan<byte> key,
        byte sequence,
        int length)
    {
        var keyLen = key.Length;
        var sequenceSalt = salts[sequence];
        var keyIndex = 0;
        var saltIndex = 0;

        for (var i = 0; i < length; i++)
        {
            var saltValue = salts[saltIndex];

            buffer[i] ^= (byte)(saltValue ^ key[keyIndex]);

            if (saltIndex != sequence)
                buffer[i] ^= sequenceSalt;

            keyIndex++;

            if (keyIndex != keyLen)
                continue;

            keyIndex = 0;
            saltIndex = (saltIndex + 1) & 0xFF;
        }
    }

    #endregion

    #region Client Encryption

    public bool IsClientEncrypted(byte opCode) => GetClientEncryptionType(opCode) != EncryptionType.None;

    public EncryptionType GetClientEncryptionType(byte opCode)
        => opCode switch
        {
            0 => EncryptionType.None,
            16 => EncryptionType.None,
            72 => EncryptionType.None,

            2 => EncryptionType.Normal,
            3 => EncryptionType.Normal,
            4 => EncryptionType.Normal,
            11 => EncryptionType.Normal,
            38 => EncryptionType.Normal,
            45 => EncryptionType.Normal,
            58 => EncryptionType.Normal,
            66 => EncryptionType.Normal,
            67 => EncryptionType.Normal,
            75 => EncryptionType.Normal,
            87 => EncryptionType.Normal,
            98 => EncryptionType.Normal,
            104 => EncryptionType.Normal,
            113 => EncryptionType.Normal,
            115 => EncryptionType.Normal,
            123 => EncryptionType.Normal,

            _ => EncryptionType.MD5
        };

    public void GenerateEncryptionParameters()
    {
        Seed = (byte)RandomNumberGenerator.GetInt32(0, 10);
        FillCryptoSeed(out var a, out var b);
        Key = GenerateKey(a, b);
    }

    /// <summary>
    /// Decrypts a packet that's been sent to a client
    /// </summary>
    public void ClientDecrypt(ref Span<byte> buffer, byte opCode, byte sequence)
    {
        var type = GetServerEncryptionType(opCode);
        if (type == EncryptionType.None)
            return;

        // Client decrypts server -> client packets. Always end with 3 seed bytes if encrypted.
        var resultLength = buffer.Length - 3;

        var a = (ushort)(((buffer[resultLength + 2] << 8) | buffer[resultLength]) ^ ClientDecryptA_Xor);
        var b = (byte)(buffer[resultLength + 1] ^ ServerB_Xor);
        var salts = Salts;
        ReadOnlySpan<byte> thisKey = type == EncryptionType.Normal ? Key : GenerateKey(a, b);

        ApplyXorCipher(buffer, salts, thisKey, sequence, resultLength);
        buffer = buffer[..resultLength];
    }

    /// <summary>
    /// Encrypts a packet that's being sent from a client
    /// </summary>
    public void ClientEncrypt(ref Span<byte> buffer, byte opcode, byte sequence)
    {
        if (opcode is 57 or 58)
        {
            EncryptDialog(ref buffer);
        }

        var type = GetClientEncryptionType(opcode);
        if (type == EncryptionType.None)
            return;

        FillCryptoSeed(out var a, out var b);

        var salts = Salts;
        ReadOnlySpan<byte> thisKey = type == EncryptionType.Normal ? Key : GenerateKey(a, b);

        // Protocol:
        // - Always include 1 extra plaintext byte (previously implicit 0) in the encrypted/hash region.
        // - If MD5 mode: also include opcode echo byte in plaintext region.
        // - Append 4 hash bytes + 3 seed bytes afterward.
        var payloadLen = buffer.Length;

        var extraPlain = 1; // protocol extra byte
        var opcodeEcho = type == EncryptionType.MD5 ? 1 : 0;
        var hashTail = 4;
        var seedTail = 3;

        var totalLen = payloadLen + extraPlain + opcodeEcho + hashTail + seedTail;

        var arr = new byte[totalLen];
        var newBuffer = arr.AsSpan();

        buffer.CopyTo(newBuffer);

        var pos = payloadLen;

        // Make the protocol byte explicit
        newBuffer[pos++] = 0x00;

        if (type == EncryptionType.MD5)
            newBuffer[pos++] = opcode;

        // Encrypt plaintext region [0..pos]
        ApplyXorCipher(newBuffer, salts, thisKey, sequence, pos);

        Span<byte> md5 = stackalloc byte[16];
        var hashLength = pos + 2;

        if (hashLength <= 512)
        {
            Span<byte> bytesToHash = stackalloc byte[hashLength];
            bytesToHash[0] = opcode;
            bytesToHash[1] = sequence;
            newBuffer[..pos].CopyTo(bytesToHash[2..]);

            if (!MD5.TryHashData(bytesToHash, md5, out _))
                throw new CryptographicException("MD5 hashing failed (packet).");
        }
        else
        {
            var rented = ArrayPool<byte>.Shared.Rent(hashLength);

            try
            {
                var bytesToHash = rented.AsSpan(0, hashLength);
                bytesToHash[0] = opcode;
                bytesToHash[1] = sequence;
                newBuffer[..pos].CopyTo(bytesToHash[2..]);

                if (!MD5.TryHashData(bytesToHash, md5, out _))
                    throw new CryptographicException("MD5 hashing failed (packet).");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        // Append selected MD5 bytes
        newBuffer[pos++] = md5[13];
        newBuffer[pos++] = md5[3];
        newBuffer[pos++] = md5[11];
        newBuffer[pos++] = md5[7];

        // Append seed bytes (client constants)
        newBuffer[pos++] = (byte)((a & 0xFF) ^ ClientAloXor);
        newBuffer[pos++] = (byte)(b ^ ClientB_Xor);
        newBuffer[pos++] = (byte)(((a >> 8) & 0xFF) ^ ClientAhiXor);

        buffer = newBuffer;
    }

    /// <summary>
    /// Encrypts a packet that's being sent from a server
    /// </summary>
    public void ServerEncrypt(ref Span<byte> buffer, byte opCode, byte sequence)
    {
        var type = GetServerEncryptionType(opCode);
        if (type == EncryptionType.None)
            return;

        FillCryptoSeed(out var a, out var b);

        var salts = Salts;
        ReadOnlySpan<byte> thisKey = type == EncryptionType.Normal ? Key : GenerateKey(a, b);

        ApplyXorCipher(buffer, salts, thisKey, sequence, buffer.Length);

        var arr = new byte[buffer.Length + 3];
        var newBuffer = arr.AsSpan();
        buffer.CopyTo(newBuffer);

        // Append seed bytes (server constants)
        newBuffer[^3] = (byte)((a & 0xFF) ^ ServerAloXor);
        newBuffer[^2] = (byte)(b ^ ServerB_Xor);
        newBuffer[^1] = (byte)(((a >> 8) & 0xFF) ^ ServerAhiXor);

        buffer = newBuffer;
    }

    /// <summary>
    /// Decrypts a packet that's been sent to a server
    /// </summary>
    public void ServerDecrypt(ref Span<byte> buffer, byte opCode, byte sequence)
    {
        var type = GetClientEncryptionType(opCode);
        if (type == EncryptionType.None)
            return;

        // baseIndex = buffer.Length - 7
        // Then adjust plaintext length by -1 (Normal) or -2 (MD5)
        var baseIndex = buffer.Length - 7;

        var a = (ushort)(((buffer[baseIndex + 6] << 8) | buffer[baseIndex + 4]) ^ ServerDecryptA_Xor);
        var b = (byte)(buffer[baseIndex + 5] ^ ClientB_Xor);
        var salts = Salts;

        var length = baseIndex;
        ReadOnlySpan<byte> thisKey;

        if (type == EncryptionType.Normal)
        {
            length -= 1; // protocol extra byte
            thisKey = Key;
        }
        else
        {
            length -= 2; // protocol extra + opcode echo
            thisKey = GenerateKey(a, b);
        }

        ApplyXorCipher(buffer, salts, thisKey, sequence, length);
        buffer = buffer[..length];

        if (opCode is 57 or 58)
            DecryptDialog(ref buffer);
    }

    #endregion

    #region Dialog Encryption

    public void EncryptDialog(ref Span<byte> buffer)
    {
        var arr = new byte[buffer.Length + 6];
        var newBuffer = arr.AsSpan();

        // payload begins at +6
        buffer.CopyTo(newBuffer[6..]);

        var checksum = Crc.Generate16(newBuffer[6..]);

        newBuffer[0] = (byte)Random.Shared.Next();
        newBuffer[1] = (byte)Random.Shared.Next();
        newBuffer[2] = (byte)((newBuffer.Length - 4) / 256);
        newBuffer[3] = (byte)((newBuffer.Length - 4) % 256);
        newBuffer[4] = (byte)(checksum / 256);
        newBuffer[5] = (byte)(checksum % 256);

        var num1 = (newBuffer[2] << 8) | newBuffer[3];
        var num2 = (byte)(newBuffer[1] ^ (uint)(byte)(newBuffer[0] - 45U));
        var num3 = (byte)(num2 + 114U);
        var num4 = (byte)(num2 + 40U);

        newBuffer[2] ^= num3;
        newBuffer[3] ^= (byte)((num3 + 1) % 256);

        for (var index = 0; index < num1; ++index)
            newBuffer[4 + index] ^= (byte)((num4 + index) % 256);

        buffer = newBuffer;
    }

    public void DecryptDialog(ref Span<byte> buffer)
    {
        var num1 = (byte)(buffer[1] ^ (uint)(byte)(buffer[0] - 45U));
        var num2 = (byte)(num1 + 114U);
        var num3 = (byte)(num1 + 40U);

        buffer[2] ^= num2;
        buffer[3] ^= (byte)(num2 + 1);
        var num4 = (buffer[2] << 8) | buffer[3];

        for (var index = 0; index < num4; index++)
            buffer[4 + index] ^= (byte)(num3 + index);

        buffer = buffer[6..];
    }

    #endregion

    #region Server Encryption Switch

    public bool IsServerEncrypted(byte opCode) => GetServerEncryptionType(opCode) != EncryptionType.None;

    public EncryptionType GetServerEncryptionType(byte opCode)
        => opCode switch
        {
            0 => EncryptionType.None,
            3 => EncryptionType.None,
            64 => EncryptionType.None,
            126 => EncryptionType.None,

            1 => EncryptionType.Normal,
            2 => EncryptionType.Normal,
            10 => EncryptionType.Normal,
            86 => EncryptionType.Normal,
            96 => EncryptionType.Normal,
            98 => EncryptionType.Normal,
            102 => EncryptionType.Normal,
            111 => EncryptionType.Normal,

            _ => EncryptionType.MD5
        };

    #endregion
}

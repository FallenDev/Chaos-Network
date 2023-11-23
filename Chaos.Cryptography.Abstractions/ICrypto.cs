using Chaos.Cryptography.Abstractions.Definitions;

namespace Chaos.Cryptography.Abstractions;

/// <summary>
///     Defines a pattern for encryption and decryption on spans using opcodes
/// </summary>
public interface ICrypto
{
    /// <summary>
    ///     Encryption key
    /// </summary>
    byte[] Key { get; }
    /// <summary>
    ///     The seed is used to generate key salts
    /// </summary>
    byte Seed { get; }

    /// <summary>
    ///     Decrypts a packet that's been sent from a client
    /// </summary>
    void Decrypt(ref Span<byte> buffer, byte opCode, byte sequence);

    /// <summary>
    ///     Decrypts the dialog header of a packet sent from a client
    /// </summary>
    void DecryptDialog(ref Span<byte> buffer);

    /// <summary>
    ///     Encrypts a packet that's being sent to a client
    /// </summary>
    void Encrypt(ref Span<byte> buffer, byte opCode, byte sequence);

    /// <summary>
    ///     Generates a random encryption key from the given values
    /// </summary>
    byte[] GenerateKey(ushort a, byte b);

    /// <summary>
    ///     Generates pseudo-random bytes from a seed
    /// </summary>
    byte[] GenerateKeySalts(string seed);

    /// <summary>
    ///     Which type of encryption, if any, should be used with the given opcode on a packet sent from the client
    /// </summary>
    EncryptionType GetClientEncryptionType(byte opCode);

    /// <summary>
    ///     Generates an md5 hash of the given string
    /// </summary>
    /// <param name="value">The string to hash</param>
    string GetMd5Hash(string value);

    /// <summary>
    ///     Which type of encryption, if any, should be used with the given opcode on a packet sent from the server
    /// </summary>
    EncryptionType ServerEncryptionType(byte opCode);

    /// <summary>
    ///     Whether or not a packet with the given opcode sent from the client should be encrypted
    /// </summary>
    bool ShouldBeEncrypted(byte opCode);

    /// <summary>
    ///     Whether or not a packet with the given opcode sent from the server should be encrypted
    /// </summary>
    bool ShouldEncrypt(byte opCode);
}
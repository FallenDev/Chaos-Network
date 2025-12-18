using System.Buffers;
using System.Runtime.CompilerServices;

namespace Chaos.Packets;

/// <summary>
///     Represents a packet of data
/// </summary>
public ref struct Packet
{
    private const byte DefaultSignature = 0xAA;

    /// <summary>
    ///     The buffer containing the packet data
    /// </summary>
    public Span<byte> Buffer;

    /// <summary>
    ///     Whether or not the packet is encrypted
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    ///     A value used to identify the type of packet and it's purpose
    /// </summary>
    public byte OpCode { get; }

    /// <summary>
    ///     A value used to ensure packets are processed in the correct order
    /// </summary>
    public byte Sequence { get; set; }

    /// <summary>
    ///     A value used to identify the start of a packet's payload
    /// </summary>
    public byte Signature { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Packet" /> struct with the specified buffer and encryption status.
    /// </summary>
    /// <param name="span">
    ///     The buffer containing the packet data.
    /// </param>
    /// <param name="isEncrypted">
    ///     <c>
    ///         true
    ///     </c>
    ///     if the packet is encrypted; otherwise,
    ///     <c>
    ///         false
    ///     </c>
    /// </param>
    public Packet(ref Span<byte> span, bool isEncrypted)
    {
        Signature = span[0];
        OpCode = span[3];
        Sequence = span[4];
        IsEncrypted = isEncrypted;

        var resultLength = span.Length - (IsEncrypted ? 5 : 4);
        Buffer = span[^resultLength..];
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Packet" /> struct with the specified operation code.
    /// </summary>
    /// <param name="opcode">
    ///     The operation code of the packet.
    /// </param>
    public Packet(byte opcode)
    {
        OpCode = opcode;
        Signature = DefaultSignature;
        Sequence = 0;
        Buffer = [];
        IsEncrypted = false;
    }

    /// <summary>
    ///     Returns the packet data as a hexadecimal string.
    /// </summary>
    /// <returns>
    ///     The packet data as a hexadecimal string.
    /// </returns>
    public string GetHexString()
    {
        var prefix = OpCode.ToString() + ": ";
        var len = Buffer.Length;

        // each byte => "AA " (3 chars), minus trailing space, plus prefix
        var chars = new char[prefix.Length + (len == 0 ? 0 : (len * 3 - 1))];
        prefix.AsSpan().CopyTo(chars);

        const string hex = "0123456789ABCDEF";
        var c = prefix.Length;

        for (var i = 0; i < len; i++)
        {
            var b = Buffer[i];
            chars[c++] = hex[b >> 4];
            chars[c++] = hex[b & 0xF];
            if (i != len - 1)
                chars[c++] = ' ';
        }

        return new string(chars);
    }

    public IMemoryOwner<byte> RentWireBuffer(out Memory<byte> slice)
    {
        var len = GetWireLength();
        var owner = MemoryPool<byte>.Shared.Rent(len);
        slice = owner.Memory.Slice(0, len);
        WriteTo(slice.Span);
        return owner;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int GetResultLength() => Buffer.Length + (IsEncrypted ? 5 : 4) - 3;

    /// <summary>
    ///     Returns the exact number of bytes written on the wire by ToMemory()/ToSpan().
    /// </summary>
    public readonly int GetWireLength() => GetResultLength() + 3;

    /// <summary>
    ///     Writes the wire-format packet into the provided destination span.
    ///     Destination must be at least GetWireLength() bytes.
    /// </summary>
    public readonly void WriteTo(Span<byte> destination)
    {
        var resultLength = GetResultLength();
        var wireLength = resultLength + 3;

        if ((uint)destination.Length < (uint)wireLength)
            throw new ArgumentException($"Destination too small. Need {wireLength} bytes.", nameof(destination));

        destination[0] = Signature;
        destination[1] = (byte)(resultLength >> 8);
        destination[2] = (byte)resultLength;
        destination[3] = OpCode;
        destination[4] = Sequence;

        // Copy payload to the tail (same behavior as ToMemory)
        if (!Buffer.IsEmpty)
            Buffer.CopyTo(destination.Slice(wireLength - Buffer.Length, Buffer.Length));
    }

    /// <summary>
    ///     Returns a string representation of the packet data as a hexadecimal string.
    /// </summary>
    /// <returns>
    ///     A string representation of the packet data as a hexadecimal string.
    /// </returns>
    public override string ToString() => GetHexString();
}
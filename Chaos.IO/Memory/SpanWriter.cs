using System.Numerics;
using System.Text;

namespace Chaos.IO.Memory;

/// <summary>
/// A ref struct for writing primitive types, strings, and other data to a <see cref="Span{T}" />.
/// </summary>
public ref struct SpanWriter
{
    private readonly Span<byte> Buffer;
    private int Position;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanWriter" /> struct.
    /// </summary>
    /// <param name="buffer">The span of bytes to write to.</param>
    public SpanWriter(Span<byte> buffer)
    {
        Buffer = buffer;
        Position = 0;
    }

    /// <summary>
    /// Writes a boolean value to the buffer.
    /// </summary>
    public void WriteBoolean(bool value) => WriteByte(value ? (byte)1 : (byte)0);

    // Write Unsigned Numeric Types
    public void WriteUInt16(ushort value)
    {
        Buffer[Position++] = (byte)(value >> 8);
        Buffer[Position++] = (byte)(value & 0xFF);
    }

    public void WriteUInt32(uint value)
    {
        Buffer[Position++] = (byte)(value >> 24);
        Buffer[Position++] = (byte)((value >> 16) & 0xFF);
        Buffer[Position++] = (byte)((value >> 8) & 0xFF);
        Buffer[Position++] = (byte)(value & 0xFF);
    }

    public void WriteUInt64(ulong value)
    {
        for (var i = 7; i >= 0; i--)
        {
            Buffer[Position++] = (byte)(value >> (i * 8));
        }
    }

    // Write Signed Numeric Types
    public void WriteInt16(short value) => WriteUInt16((ushort)value);

    public void WriteInt32(int value) => WriteUInt32((uint)value);

    public void WriteInt64(long value) => WriteUInt64((ulong)value);

    // Write Floating Point Types
    public void WriteFloat(float value) => WriteInt32(BitConverter.SingleToInt32Bits(value));

    public void WriteDouble(double value) => WriteInt64(BitConverter.DoubleToInt64Bits(value));

    // Write Vectors
    public void WriteVector2(Vector2 value)
    {
        WriteFloat(value.X);
        WriteFloat(value.Y);
    }

    public void WriteVector3(Vector3 value)
    {
        WriteFloat(value.X);
        WriteFloat(value.Y);
        WriteFloat(value.Z);
    }

    // Write Points
    public void WritePoint8(byte x, byte y)
    {
        WriteByte(x);
        WriteByte(y);
    }

    public void WritePoint16(short x, short y)
    {
        WriteInt16(x);
        WriteInt16(y);
    }

    public void WritePoint16(ushort x, ushort y)
    {
        WriteUInt16(x);
        WriteUInt16(y);
    }

    public void WritePoint32(int x, int y)
    {
        WriteInt32(x);
        WriteInt32(y);
    }

    public void WritePoint32(uint x, uint y)
    {
        WriteUInt32(x);
        WriteUInt32(y);
    }

    public void WritePoint64(long x, long y)
    {
        WriteInt64(x);
        WriteInt64(y);
    }

    public void WritePoint64(ulong x, ulong y)
    {
        WriteUInt64(x);
        WriteUInt64(y);
    }

    // Write String with Dynamic Length Prefix
    public void WriteString(string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        if (bytes.Length <= byte.MaxValue)
        {
            WriteString8(value);
        }
        else
        {
            WriteString16(value);
        }
    }

    // Write String with 8-bit Length Prefix
    public void WriteString8(string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        if (bytes.Length > byte.MaxValue)
            throw new ArgumentException("String is too long for 8-bit length prefix.");

        WriteByte((byte)bytes.Length);
        WriteBytes(bytes);
    }

    // Write String with 16-bit Length Prefix
    public void WriteString16(string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        if (bytes.Length > ushort.MaxValue)
            throw new ArgumentException("String is too long for 16-bit length prefix.");

        WriteUInt16((ushort)bytes.Length);
        WriteBytes(bytes);
    }

    // Write Bytes
    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        bytes.CopyTo(Buffer[Position..]);
        Position += bytes.Length;
    }

    public void WriteBytes(params byte[] bytes)
    {
        foreach (var b in bytes)
        {
            WriteByte(b);
        }
    }

    // Write Raw Data
    public void WriteData16(ReadOnlySpan<byte> data)
    {
        if (data.Length > ushort.MaxValue)
            throw new ArgumentException("Data too large for a 16-bit length prefix.");

        WriteUInt16((ushort)data.Length);
        WriteBytes(data);
    }

    public void WriteData8(ReadOnlySpan<byte> data)
    {
        if (data.Length > byte.MaxValue)
            throw new ArgumentException("Data too large for an 8-bit length prefix.");

        WriteByte((byte)data.Length);
        WriteBytes(data);
    }

    public void WriteData(ReadOnlySpan<byte> data)
    {
        if (data.Length <= byte.MaxValue)
        {
            WriteData8(data);
        }
        else if (data.Length <= ushort.MaxValue)
        {
            WriteData16(data);
        }
        else
        {
            throw new ArgumentException("Data too large to serialize dynamically.");
        }
    }

    // Write Arguments
    public void WriteArgs8(List<string> args)
    {
        foreach (var arg in args)
        {
            WriteString8(arg);
        }
    }

    public void WriteArgs(List<string> args)
    {
        foreach (var arg in args)
        {
            WriteString(arg);
        }
    }

    // Write Single Byte
    public void WriteByte(byte value) => Buffer[Position++] = value;

    // Write Signed Byte
    public void WriteSByte(sbyte value) => Buffer[Position++] = (byte)value;

    /// <summary>
    /// Trims the buffer to the written content and returns it as a span.
    /// </summary>
    /// <returns>A span containing the written content.</returns>
    public Span<byte> ToSpan() => Buffer[..Position];
}

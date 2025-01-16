using System.Numerics;
using System.Text;

namespace Chaos.IO.Memory;

/// <summary>
/// A ref struct for writing primitive types, strings, and other data to a <see cref="Span{T}" />.
/// </summary>
public ref struct SpanWriter
{
    private Span<byte> Buffer;
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
    /// Gets a value indicating whether the writer has reached or exceeded the end of the span.
    /// </summary>
    public bool EndOfSpan => Position >= Buffer.Length;

    /// <summary>
    /// Ensures there is enough space to write the specified number of bytes.
    /// If not, resizes the buffer dynamically.
    /// </summary>
    /// <param name="count">The number of bytes to check for.</param>
    private void EnsureCapacity(int count)
    {
        if (Position + count > Buffer.Length)
        {
            ResizeBuffer(Position + count);
        }
    }

    /// <summary>
    /// Dynamically resizes the buffer to accommodate additional data.
    /// </summary>
    /// <param name="requiredSize">The minimum required size for the buffer.</param>
    private void ResizeBuffer(int requiredSize)
    {
        var newSize = Math.Max(Buffer.Length * 2, requiredSize);
        var newBuffer = new byte[newSize];
        Buffer[..Position].CopyTo(newBuffer);
        Buffer = newBuffer;
    }

    /// <summary>
    /// Writes a boolean value to the buffer.
    /// </summary>
    public void WriteBoolean(bool value) => WriteByte(value ? (byte)1 : (byte)0);

    // Write Unsigned Numeric Types
    public void WriteUInt16(ushort value)
    {
        EnsureCapacity(2);
        Buffer[Position++] = (byte)(value >> 8);
        Buffer[Position++] = (byte)(value & 0xFF);
    }

    public void WriteUInt32(uint value)
    {
        EnsureCapacity(4);
        Buffer[Position++] = (byte)(value >> 24);
        Buffer[Position++] = (byte)((value >> 16) & 0xFF);
        Buffer[Position++] = (byte)((value >> 8) & 0xFF);
        Buffer[Position++] = (byte)(value & 0xFF);
    }

    public void WriteUInt64(ulong value)
    {
        EnsureCapacity(8);
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
        var bytes = Encoding.UTF8.GetBytes(value);
        switch (bytes.Length)
        {
            case <= byte.MaxValue:
                WriteByte(0);
                WriteString8(bytes);
                break;
            case <= ushort.MaxValue:
                WriteByte(1);
                WriteString16(bytes);
                break;
            default:
                WriteByte(2);
                WriteString32(bytes);
                break;
        }
    }

    // Write String with 8-bit Length Prefix
    private void WriteString8(byte[] bytes)
    {
        if (bytes.Length > byte.MaxValue)
        {
            WriteString16(bytes);
            return;
        }

        WriteByte((byte)bytes.Length);
        WriteBytes(bytes);
    }

    // Write String with 16-bit Length Prefix
    private void WriteString16(byte[] bytes)
    {
        if (bytes.Length > ushort.MaxValue)
        {
            WriteString32(bytes);
            return;
        }

        WriteUInt16((ushort)bytes.Length);
        WriteBytes(bytes);
    }

    // Write String with 32-bit Length Prefix
    private void WriteString32(byte[] bytes)
    {
        WriteUInt32((uint)bytes.Length);
        WriteBytes(bytes);
    }

    // Write Bytes
    public void WriteBytes(params byte[] bytes)
    {
        foreach (var b in bytes)
        {
            WriteByte(b);
        }
    }

    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        EnsureCapacity(bytes.Length);
        bytes.CopyTo(Buffer[Position..]);
        Position += bytes.Length;
    }

    // Write Raw Data
    private void WriteData8(ReadOnlySpan<byte> data)
    {
        WriteByte((byte)data.Length);
        WriteBytes(data);
    }

    private void WriteData16(ReadOnlySpan<byte> data)
    {
        WriteUInt16((ushort)data.Length);
        WriteBytes(data);
    }

    private void WriteData32(ReadOnlySpan<byte> data)
    {
        WriteUInt32((uint)data.Length);
        WriteBytes(data);
    }

    public void WriteData(ReadOnlySpan<byte> data)
    {
        switch (data.Length)
        {
            case <= byte.MaxValue:
                WriteByte(0);
                WriteData8(data);
                break;
            case <= ushort.MaxValue:
                WriteByte(1);
                WriteData16(data);
                break;
            default:
                WriteByte(2);
                WriteData32(data);
                break;
        }
    }

    // Write Arguments
    public void WriteArgs8(List<string> args)
    {
        foreach (var arg in args)
        {
            WriteString(arg);
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
    public void WriteByte(byte value)
    {
        EnsureCapacity(1);
        Buffer[Position++] = value;
    }

    // Write Signed Byte
    public void WriteSByte(sbyte value)
    {
        EnsureCapacity(1);
        Buffer[Position++] = (byte)value;
    }

    /// <summary>
    /// Trims the buffer to the written content and returns it as a span.
    /// </summary>
    /// <returns>A span containing the written content.</returns>
    public Span<byte> ToSpan() => Buffer[..Position];
}

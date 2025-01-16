using System.Text;

namespace Chaos.IO.Memory;

public static class SpanExtensions
{
    public static Span<byte> ToSpan(this string value, Encoding encoding = null)
    {
        encoding ??= Encoding.ASCII; // Default to ASCII if no encoding is provided
        var bytes = encoding.GetBytes(value);
        return new Span<byte>(bytes);
    }

    public static Span<byte> ToSpan(this bool value)
    {
        // Convert boolean to a single byte (1 for true, 0 for false)
        return new Span<byte>(new[] { value ? (byte)1 : (byte)0 });
    }

    public static Span<byte> ToSpan(this byte value)
    {
        // Single byte span
        return new Span<byte>(new[] { value });
    }

    public static Span<byte> ToSpan(this sbyte value)
    {
        // Single signed byte span
        return new Span<byte>(new[] { (byte)value });
    }

    public static Span<byte> ToSpan(this ushort value)
    {
        var buffer = new byte[2];
        buffer[0] = (byte)(value >> 8); // High byte
        buffer[1] = (byte)(value & 0xFF); // Low byte
        return new Span<byte>(buffer);
    }

    public static Span<byte> ToSpan(this short value)
    {
        var buffer = new byte[2];
        buffer[0] = (byte)(value >> 8); // High byte
        buffer[1] = (byte)(value & 0xFF); // Low byte
        return new Span<byte>(buffer);
    }

    public static Span<byte> ToSpan(this uint value)
    {
        var buffer = new byte[4];
        buffer[0] = (byte)(value >> 24); // Most significant byte
        buffer[1] = (byte)((value >> 16) & 0xFF);
        buffer[2] = (byte)((value >> 8) & 0xFF);
        buffer[3] = (byte)(value & 0xFF); // Least significant byte
        return new Span<byte>(buffer);
    }

    public static Span<byte> ToSpan(this int value)
    {
        var buffer = new byte[4];
        buffer[0] = (byte)(value >> 24); // Most significant byte
        buffer[1] = (byte)((value >> 16) & 0xFF);
        buffer[2] = (byte)((value >> 8) & 0xFF);
        buffer[3] = (byte)(value & 0xFF); // Least significant byte
        return new Span<byte>(buffer);
    }

    public static Span<byte> ToSpan(this ulong value)
    {
        var buffer = new byte[8];
        for (int i = 7; i >= 0; i--)
        {
            buffer[i] = (byte)(value & 0xFF);
            value >>= 8;
        }
        return new Span<byte>(buffer);
    }

    public static Span<byte> ToSpan(this long value)
    {
        var buffer = new byte[8];
        for (int i = 7; i >= 0; i--)
        {
            buffer[i] = (byte)(value & 0xFF);
            value >>= 8;
        }
        return new Span<byte>(buffer);
    }

    public static Span<byte> ToSpan(this float value)
    {
        var intValue = BitConverter.SingleToInt32Bits(value);
        return intValue.ToSpan();
    }

    public static Span<byte> ToSpan(this double value)
    {
        var longValue = BitConverter.DoubleToInt64Bits(value);
        return longValue.ToSpan();
    }
}

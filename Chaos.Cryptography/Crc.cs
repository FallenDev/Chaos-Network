using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Chaos.Cryptography;

[ExcludeFromCodeCoverage]
public static class Crc
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort Generate16(ReadOnlySpan<byte> data)
    {
        uint checkSum = 0;
        var table = Tables.TABLE16;

        for (var i = 0; i < data.Length; i++)
            checkSum = (uint)(data[i] ^ (checkSum << 8) ^ table[(int)(checkSum >> 8)]);

        return (ushort)checkSum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Generate32(ReadOnlySpan<byte> data)
    {
        uint checkSum = uint.MaxValue;
        var table = Tables.TABLE32;

        for (var i = 0; i < data.Length; i++)
            checkSum = (checkSum >> 8) ^ table[(int)((checkSum & 0xFF) ^ data[i])];

        return ~checkSum;
    }
}
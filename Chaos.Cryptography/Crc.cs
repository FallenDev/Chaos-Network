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

        for (var i = 0; i < data.Length; ++i)
            checkSum = (ushort)(data[i] ^ (checkSum << 8) ^ Tables.TABLE16[(int)(checkSum >> 8)]);

        return (ushort)checkSum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Generate32(ReadOnlySpan<byte> data)
    {
        var checkSum = uint.MaxValue;

        for (var i = 0; i < data.Length; ++i)
            checkSum = (checkSum >> 8) ^ Tables.TABLE32[(int)((checkSum & byte.MaxValue) ^ data[i])];

        return ~checkSum;
    }
}
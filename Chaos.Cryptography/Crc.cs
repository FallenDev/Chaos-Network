using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Chaos.Cryptography;

[ExcludeFromCodeCoverage]
public static class Crc
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort Generate16(ReadOnlySpan<byte> data)
    {
        ushort crc = 0;
        var table = Tables.TABLE16;

        for (int i = 0; i < data.Length; i++)
        {
            // top byte of crc xor next data byte gives 0..255
            int idx = ((crc >> 8) ^ data[i]) & 0xFF;
            crc = (ushort)((crc << 8) ^ table[idx]);
        }

        return crc;
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
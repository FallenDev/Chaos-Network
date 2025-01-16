using System.Numerics;
using System.Text;

namespace Chaos.IO.Memory
{
    public ref struct SpanReader
    {
        private ReadOnlySpan<byte> Buffer;
        private int Position;

        public SpanReader(ReadOnlySpan<byte> buffer)
        {
            Buffer = buffer;
            Position = 0;
        }

        // Read Boolean
        public bool ReadBoolean() => ReadByte() != 0;

        // Read Unsigned Numeric Types
        public ushort ReadUInt16() => (ushort)((ReadByte() << 8) | ReadByte());

        public uint ReadUInt32() =>
            (uint)((ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());

        public ulong ReadUInt64()
        {
            ulong value = 0;
            for (var i = 0; i < 8; i++)
            {
                value = (value << 8) | ReadByte();
            }
            return value;
        }

        // Read Signed Numeric Types
        public short ReadInt16() => (short)ReadUInt16();

        public int ReadInt32() => (int)ReadUInt32();

        public long ReadInt64() => (long)ReadUInt64();

        // Read Floating Point Types
        public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt32());

        public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadInt64());

        // Read Vectors
        public Vector2 ReadVector2() => new(ReadFloat(), ReadFloat());

        public Vector3 ReadVector3() => new(ReadFloat(), ReadFloat(), ReadFloat());

        // Read Points
        public (byte x, byte y) ReadPoint8() => (ReadByte(), ReadByte());

        public (short x, short y) ReadPoint16() => (ReadInt16(), ReadInt16());

        public (ushort x, ushort y) ReadPoint16U() => (ReadUInt16(), ReadUInt16());

        public (int x, int y) ReadPoint32() => (ReadInt32(), ReadInt32());

        public (uint x, uint y) ReadPoint32U() => (ReadUInt32(), ReadUInt32());

        public (long x, long y) ReadPoint64() => (ReadInt64(), ReadInt64());

        public (ulong x, ulong y) ReadPoint64U() => (ReadUInt64(), ReadUInt64());

        // Read String
        public string ReadString(bool is16BitLength = false) => is16BitLength ? ReadString16() : ReadString8();

        public string ReadString8()
        {
            var length = ReadByte();
            return ReadString(length);
        }

        public string ReadString16()
        {
            var length = ReadUInt16();
            return ReadString(length);
        }

        private string ReadString(int length)
        {
            if (Position + length > Buffer.Length)
                throw new IndexOutOfRangeException("String length exceeds available buffer.");

            var result = Encoding.ASCII.GetString(Buffer.Slice(Position, length));
            Position += length;
            return result;
        }

        // Read Data
        public ReadOnlySpan<byte> ReadData() => ReadBytesAsSpan(Remaining);

        public ReadOnlySpan<byte> ReadData8()
        {
            var length = ReadByte();
            return ReadBytesAsSpan(length);
        }

        public ReadOnlySpan<byte> ReadData16()
        {
            var length = ReadUInt16();
            return ReadBytesAsSpan(length);
        }

        // Read Bytes
        public ReadOnlySpan<byte> ReadBytesAsSpan(int length)
        {
            if (Position + length > Buffer.Length)
                throw new IndexOutOfRangeException("Requested length exceeds available buffer.");

            var result = Buffer.Slice(Position, length);
            Position += length;
            return result;
        }

        public byte[] ReadBytes(int length) => ReadBytesAsSpan(length).ToArray();

        // Read Arguments
        public List<string> ReadArgs()
        {
            var args = new List<string>();

            while (Position < Buffer.Length)
                args.Add(ReadString());

            return args;
        }

        public List<string> ReadArgs8()
        {
            var args = new List<string>();

            while (Position < Buffer.Length)
                args.Add(ReadString8());

            return args;
        }

        // Read Single Byte
        public byte ReadByte() => Buffer[Position++];

        // Read Signed Byte
        public sbyte ReadSByte() => (sbyte)Buffer[Position++];

        public ReadOnlySpan<byte> ToSpan()
        {
            if (Position > Buffer.Length)
                throw new IndexOutOfRangeException("Position exceeds buffer length.");

            return Buffer[Position..];
        }

        public int Remaining => Buffer.Length - Position;

        /// <summary>
        /// Gets a value indicating whether the writer has reached or exceeded the end of the span.
        /// </summary>
        public bool EndOfSpan => Position >= Buffer.Length;
    }
}

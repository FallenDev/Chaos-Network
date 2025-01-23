using System.Text;

using Microsoft.Extensions.Logging;

namespace Chaos.Packets
{
    /// <summary>
    /// Represents a packet of data in the custom application protocol.
    /// </summary>
    public ref struct Packet
    {
        public byte Signature { get; }
        public ushort Length { get; }
        public byte OpCode { get; }
        public byte Sequence { get; set; }
        public Span<byte> Payload { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Packet"/> struct from a span of bytes.
        /// </summary>
        /// <param name="span">The buffer containing the packet data.</param>
        public Packet(ref Span<byte> span)
        {
            if (span.Length < 5)
                throw new ArgumentException("Span is too short to be a valid packet.");

            Signature = span[0];

            // Extract length (big-endian)
            var payloadLength = (span[1] << 8) | span[2];

            // Validate total packet size
            if (span.Length != 5 + payloadLength)
            {
                throw new ArgumentException($"Span length ({span.Length}) does not match the total packet size (5 + {payloadLength}).");
            }

            OpCode = span[3];
            Sequence = span[4];

            // Extract the payload
            Payload = span.Slice(5, payloadLength);
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="Packet"/> struct with specified values.
        /// </summary>
        /// <param name="opCode">The operation code of the packet.</param>
        /// <param name="sequence">The sequence number of the packet.</param>
        /// <param name="payload">The payload data.</param>
        public Packet(byte opCode, byte sequence, Span<byte> payload)
        {
            Signature = 0x16; // Custom signature
            Length = (ushort)payload.Length;
            OpCode = opCode;
            Sequence = sequence;
            Payload = payload;
        }

        public Packet(byte opCode, Span<byte> payload)
        {
            Signature = 0x16; // Custom signature
            Length = (ushort)payload.Length;
            OpCode = opCode;
            Sequence = 0; // Default sequence
            Payload = payload;
        }


        /// <summary>
        /// Converts the packet to a byte array.
        /// </summary>
        /// <returns>The packet as a byte array.</returns>
        public byte[] ToArray()
        {
            var buffer = new byte[5 + Payload.Length];
            buffer[0] = Signature;
            buffer[1] = (byte)(Length >> 8);
            buffer[2] = (byte)(Length & 0xFF);
            buffer[3] = OpCode;
            buffer[4] = Sequence;
            Payload.CopyTo(buffer.AsSpan(5));
            return buffer;
        }

        /// <summary>
        /// Returns the payload as an ASCII string.
        /// </summary>
        /// <param name="replaceNewline">Whether to replace newline characters with spaces.</param>
        /// <returns>The payload as an ASCII string.</returns>
        public string GetAsciiString(bool replaceNewline = true)
        {
            var str = Encoding.ASCII.GetString(Payload);
            return replaceNewline ? str.Replace('\n', ' ').Replace('\r', ' ') : str;
        }

        /// <summary>
        /// Returns a hexadecimal representation of the packet.
        /// </summary>
        public override string ToString()
        {
            return $"{OpCode}: {BitConverter.ToString(Payload.ToArray()).Replace("-", " ")}";
        }
    }
}

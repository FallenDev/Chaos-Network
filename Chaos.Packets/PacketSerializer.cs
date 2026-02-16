using System.Buffers;
using System.Collections.Frozen;
using System.Text;

using Chaos.IO.Memory;
using Chaos.Packets.Abstractions;

namespace Chaos.Packets;

/// <summary>
/// Serializes/deserializes packet payloads.
/// </summary>
public sealed class PacketSerializer : IPacketSerializer, IPooledPacketSerializer
{
    private readonly FrozenDictionary<byte, IPacketConverter> _byOpCode;
    private readonly FrozenDictionary<Type, IPacketConverter> _byType;
    private static readonly ArrayPool<byte> PayloadPool = ArrayPool<byte>.Shared;

    public Encoding Encoding { get; }

    public PacketSerializer(IEnumerable<IPacketConverter> converters, Encoding encoding)
    {
        Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));

        var opMap = new Dictionary<byte, IPacketConverter>();
        var typeMap = new Dictionary<Type, IPacketConverter>();

        foreach (var c in converters)
        {
            opMap[c.OpCode] = c;

            var generic = c.GetType()
                .GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IPacketConverter<>));

            if (generic != null)
            {
                var target = generic.GetGenericArguments()[0];
                typeMap[target] = c;
            }
        }

        _byOpCode = opMap.ToFrozenDictionary();
        _byType = typeMap.ToFrozenDictionary();
    }

    public T Deserialize<T>(in Packet packet) where T : IPacketSerializable
    {
        if (!_byOpCode.TryGetValue(packet.OpCode, out var converter))
            throw new InvalidOperationException(
                $"No converter registered for opcode {packet.OpCode}");

        var reader = new SpanReader(Encoding, in packet.Buffer);

        var result = converter.Deserialize(ref reader);
        return (T)result;
    }

    public Packet Serialize(IPacketSerializable obj)
    {
        if (!_byType.TryGetValue(obj.GetType(), out var converter))
            throw new InvalidOperationException(
                $"No converter registered for type {obj.GetType().FullName}");

        var writer = new SpanWriter(Encoding);
        converter.Serialize(ref writer, obj);
        var payload = writer.ToSpan();

        var packet = new Packet(converter.OpCode)
        {
            Buffer = payload
        };

        return packet;
    }

    public bool TrySerializePooled(IPacketSerializable obj, out byte opCode, out byte[] payloadBuffer, out int payloadLength)
    {
        opCode = 0;
        payloadBuffer = Array.Empty<byte>();
        payloadLength = 0;

        if (!_byType.TryGetValue(obj.GetType(), out var converter))
            return false;

        opCode = converter.OpCode;

        // Aggressive: avoid SpanWriter auto-grow allocations by retrying with larger pooled buffers.
        var size = 256;
        while (true)
        {
            var rented = PayloadPool.Rent(size);

            try
            {
                var span = rented.AsSpan();
                var writer = new SpanWriter(Encoding, ref span);
                converter.Serialize(ref writer, obj);

                payloadLength = writer.Position;
                payloadBuffer = rented;
                return true;
            }
            catch (EndOfStreamException)
            {
                PayloadPool.Return(rented);
                size <<= 1;
                if (size > 64 * 1024)
                    throw;
            }
            catch
            {
                PayloadPool.Return(rented);
                throw;
            }
        }
    }

    public void ReturnPooled(byte[] payloadBuffer)
    {
        if (payloadBuffer.Length > 0)
            PayloadPool.Return(payloadBuffer);
    }
}

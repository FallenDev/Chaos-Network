using System.Collections.Frozen;
using System.Text;

using Chaos.IO.Memory;
using Chaos.Packets.Abstractions;

namespace Chaos.Packets;

/// <summary>
/// Serializes/deserializes packet payloads.
/// </summary>
public sealed class PacketSerializer : IPacketSerializer
{
    private readonly FrozenDictionary<byte, IPacketConverter> _byOpCode;
    private readonly FrozenDictionary<Type, IPacketConverter> _byType;

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
}

using Chaos.IO.Memory;

namespace Chaos.Packets.Abstractions;

/// <summary>
///     A base packet converter that forwards non-generic methods to the associated generic methods
/// </summary>
/// <typeparam name="T">
///     The serializable type the converter is for
/// </typeparam>
public abstract class PacketConverterBase<T> : IPacketConverter<T> where T : IPacketSerializable
{
    public abstract byte OpCode { get; }
    object IPacketConverter.Deserialize(ref SpanReader reader) => Deserialize(ref reader);
    public virtual T Deserialize(ref SpanReader reader) => default!;
    public virtual void Serialize(ref SpanWriter writer, T args) { }
    void IPacketConverter.Serialize(ref SpanWriter writer, object args) => Serialize(ref writer, (T)args);
}
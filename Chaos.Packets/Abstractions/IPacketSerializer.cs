using System.Text;

namespace Chaos.Packets.Abstractions;

/// <summary>
///     Defines a contract for a packet serializer used to serialize and deserialize packets.
/// </summary>
public interface IPacketSerializer
{
    /// <summary>
    ///     Gets the encoding used by the serializer.
    /// </summary>
    Encoding Encoding { get; }

    /// <summary>
    ///     Deserializes the specified client packet into an instance of <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of object to deserialize the packet into, which must implement <see cref="IReceiveArgs" />
    ///     .
    /// </typeparam>
    /// <param name="packet">The client packet to deserialize.</param>
    /// <returns>An instance of <typeparamref name="T" /> deserialized from the client packet.</returns>
    T Deserialize<T>(in ClientPacket packet) where T: IReceiveArgs;

    /// <summary>
    ///     Serializes the specified object implementing <see cref="ISendArgs" /> into a server packet.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize, which must implement <see cref="ISendArgs" />.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>A server packet representing the serialized <paramref name="obj" />.</returns>
    ServerPacket Serialize<T>(T obj) where T: ISendArgs;
}
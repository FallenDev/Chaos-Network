namespace Chaos.Packets.Abstractions;

public interface IPooledPacketSerializer
{
    bool TrySerializePooled(
        IPacketSerializable obj,
        out byte opCode,
        out byte[] payloadBuffer,
        out int payloadLength);

    void ReturnPooled(byte[] payloadBuffer);
}

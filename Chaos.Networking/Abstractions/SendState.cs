namespace Chaos.Networking.Abstractions;

public sealed class SendState
{
    public byte[] Buffer = [];
    public int Offset;
    public int Remaining;
}

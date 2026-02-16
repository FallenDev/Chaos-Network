namespace Chaos.Networking.Abstractions;

public sealed class SendState
{
    public byte[] Buffer = default!;
    public int Offset;
    public int Remaining;
}

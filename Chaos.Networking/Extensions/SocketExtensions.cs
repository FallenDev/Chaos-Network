using System.Buffers.Binary;
using System.Net.Sockets;

namespace Chaos.Networking.Extensions;

internal static class SocketExtensions
{
    internal static void ConfigureTcpSocket(Socket tcpSocket)
    {
        // The socket will not linger when Socket.Close is called
        tcpSocket.LingerState = new LingerOption(false, 0);

        // Disable the Nagle Algorithm for low-latency communication
        tcpSocket.NoDelay = true;

        // Kernel buffers sized for typical game traffic (tuned separately from app-level buffers)
        tcpSocket.ReceiveBufferSize = 64 * 1024;
        tcpSocket.SendBufferSize = 64 * 1024;

        // Enable TCP keep-alive to detect stale connections
        tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

        // Tune keep-alive settings (Windows)
        try
        {
            // SIO_KEEPALIVE_VALS = 0x98000004
            const int SioKeepAliveVals = unchecked((int)0x98000004);

            Span<byte> inOptionValues = stackalloc byte[12];

            // Windows expects little-endian DWORD values for SIO_KEEPALIVE_VALS
            BinaryPrimitives.WriteUInt32LittleEndian(inOptionValues[..4], 1u);          // onOff
            BinaryPrimitives.WriteUInt32LittleEndian(inOptionValues.Slice(4, 4), 30_000u); // time (ms)
            BinaryPrimitives.WriteUInt32LittleEndian(inOptionValues.Slice(8, 4), 10_000u); // interval (ms)

            // IOControl requires byte[], so copy once into a small array for interop call
            tcpSocket.IOControl(SioKeepAliveVals, inOptionValues.ToArray(), null);
        }
        catch { }
    }
}
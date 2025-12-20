using System.Net.Sockets;

namespace Chaos.Extensions.Networking;

internal static class SocketExtensions
{
    internal static void ReceiveAndForget(this Socket socket, SocketAsyncEventArgs args, EventHandler<SocketAsyncEventArgs> completedEvent)
    {
        //if we receive true, it means the io operation is pending, and the completion will be raised on the args completed event
        var completedSynchronously = !socket.ReceiveAsync(args);

        //if we receive false, it means the io operation completed synchronously, and the completed event will not be raised
        if (completedSynchronously)
            completedEvent(socket, args);
    }

    internal static void SendAndForget(this Socket socket, SocketAsyncEventArgs args, EventHandler<SocketAsyncEventArgs> completedEvent)
    {
        var completedSynchronously = !socket.SendAsync(args);

        if (completedSynchronously)
            completedEvent(socket, args);
    }

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

            var inOptionValues = new byte[12];
            // onOff (1 = true)
            BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
            // Time (ms) before first probe
            BitConverter.GetBytes((uint)30_000).CopyTo(inOptionValues, 4);
            // Interval (ms) between probes
            BitConverter.GetBytes((uint)10_000).CopyTo(inOptionValues, 8);
            // Apply keep-alive settings
            tcpSocket.IOControl(SioKeepAliveVals, inOptionValues, null);
        }
        catch { }
    }
}
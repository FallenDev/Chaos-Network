using System.Net.Sockets;

namespace Chaos.Extensions.Networking;

internal static class SocketExtensions
{
    internal static void SendAndForget(this Socket socket, SocketAsyncEventArgs args, EventHandler<SocketAsyncEventArgs> completedEvent)
    {
        try
        {
            if (!socket.SendAsync(args))
                completedEvent(socket, args);
        }
        catch
        {
            // If SendAsync throws, Completed will not fire. Ensure resources return to pool.
            completedEvent(socket, args);
            throw;
        }
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
    }
}
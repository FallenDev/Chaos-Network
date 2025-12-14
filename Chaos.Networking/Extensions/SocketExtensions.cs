using System.Net.Sockets;

// ReSharper disable once CheckNamespace
namespace Chaos.Extensions.Networking;

internal static class SocketExtensions
{
    internal static void ReceiveAndForget(this Socket socket, SocketAsyncEventArgs args, EventHandler<SocketAsyncEventArgs> completedEvent)
    {
        try
        {
            if (!socket.ReceiveAsync(args))
                completedEvent(socket, args);
        }
        catch
        {
            // If ReceiveAsync throws, Completed will not fire. Ensure resources return to pool.
            completedEvent(socket, args);
            throw;
        }
    }

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
}
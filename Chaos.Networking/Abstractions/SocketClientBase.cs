using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Chaos.Common.Identity;
using Chaos.Packets;
using Chaos.Packets.Abstractions;

using Microsoft.Extensions.Logging;

namespace Chaos.Networking.Abstractions;

/// <summary>
///     Provides the ability to send and receive packets over a socket.
/// </summary>
public abstract class SocketClientBase : ISocketClient, IDisposable
{
    private readonly SslStream SslStream;

    /// <summary>
    ///     The server certificate for SSL/TLS encryption.
    /// </summary>
    public X509Certificate2 ServerCertificate { get; private set; }

    private int Count;
    private int Sequence;

    /// <inheritdoc />
    public bool Connected { get; set; }


    /// <inheritdoc />
    public uint Id { get; }

    /// <summary>
    ///     The logger for logging client-related events.
    /// </summary>
    protected ILogger<SocketClientBase> Logger { get; }

    private MemoryHandle MemoryHandle { get; }

    private IMemoryOwner<byte> MemoryOwner { get; }

    /// <summary>
    ///     The packet serializer for serializing and deserializing packets.
    /// </summary>
    protected IPacketSerializer PacketSerializer { get; }
    
    /// <inheritdoc />
    public IPAddress RemoteIp { get; }

    /// <inheritdoc />
    public Socket Socket { get; }

    private Memory<byte> Memory => MemoryOwner.Memory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SocketClientBase" /> class.
    /// </summary>
    /// <param name="socket"></param>
    /// <param name="packetSerializer"></param>
    /// <param name="logger"></param>
    protected SocketClientBase(
        Socket socket,
        IPacketSerializer packetSerializer,
        ILogger<SocketClientBase> logger)
    {
        Id = SequentialIdGenerator<uint>.Shared.NextId;
        Socket = socket;
        ServerCertificate = LoadCertificateFromStore();

        // Wrap the socket in an SslStream
        SslStream = new SslStream(new NetworkStream(socket), false);

        // Authenticate as client (or server depending on the use case)
        SslStream.AuthenticateAsServer(ServerCertificate, false, SslProtocols.Tls12, true);

        MemoryOwner = MemoryPool<byte>.Shared.Rent(ushort.MaxValue * 4);
        MemoryHandle = Memory.Pin();
        Logger = logger;
        PacketSerializer = packetSerializer;
        RemoteIp = (Socket.RemoteEndPoint as IPEndPoint)?.Address ?? IPAddress.None;
        Connected = false;
    }

    private static X509Certificate2 LoadCertificateFromStore()
    {
        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);

        var certs = store.Certificates.Find(X509FindType.FindBySubjectName, "localhost", validOnly: false).Where(c => c.FriendlyName == "ZolianAuth").ToList();

        if (certs.Count > 0)
        {
            return certs[0];
        }

        throw new Exception("Certificate not found in the store.");
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        SslStream.Dispose();
        GC.SuppressFinalize(this);

        try
        {
            MemoryHandle.Dispose();
        }
        catch
        {
            //ignored
        }

        try
        {
            MemoryOwner.Dispose();
        }
        catch
        {
            //ignored
        }

        try
        {
            Socket.Close();
        }
        catch
        {
            //ignored
        }
    }

    public event EventHandler? OnDisconnected;

    public virtual void SetSequence(byte newSequence) => Sequence = newSequence;
    

    /// <summary>
    ///     Asynchronously handles a span buffer as a packet.
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    protected abstract ValueTask HandlePacketAsync(Span<byte> span);

    #region Networking

    public virtual async void BeginReceive()
    {
        if (!Socket.Connected) return;
        Connected = true;
        await Task.Yield();

        try
        {
            while (Connected)
            {
                // Read decrypted data directly from SslStream
                var bytesRead = await SslStream.ReadAsync(Memory).ConfigureAwait(false);

                if (bytesRead > 0)
                {
                    // Process the data using ReceiveEventHandler logic
                    await ProcessDecryptedData(bytesRead).ConfigureAwait(false);
                }
                else
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error - Disconnecting Client. {RemoteIp}", RemoteIp);
            Disconnect();
        }
    }

    private async Task ProcessDecryptedData(int bytesRead)
    {
        try
        {
            var memorySlice = Memory[..bytesRead];
            Count += bytesRead;
            var offset = 0;

            while (Count > 4) // Ensure there’s enough for Signature, Length, OpCode, and Sequence
            {
                // Verify signature
                if (memorySlice.Span[offset] != 0x16) // 22
                {
                    Logger.LogWarning("Invalid packet signature at offset {Offset}. Disconnecting client. {RemoteIP}", offset, RemoteIp);
                    Disconnect();
                    return;
                }

                // Extract length
                var length = (memorySlice.Span[offset + 1] << 8) | memorySlice.Span[offset + 2]; // Big-endian
                var packetLength = length + 5; // Include Signature, Length, OpCode, Sequence

                // Wait for the rest of the packet to arrive
                if (Count < packetLength) break;

                try
                {
                    // Extract and process the complete packet
                    var packetSpan = memorySlice.Span.Slice(offset, packetLength);
                    await HandlePacketAsync(packetSpan).ConfigureAwait(false);
                }
                catch
                {
                    Count = 0; // Reset count if there's a processing error
                    break;
                }

                Count -= packetLength;
                offset += packetLength;
            }

            if (Count > 0)
            {
                // Move remaining data to the start of the buffer
                memorySlice.Slice(offset, Count).CopyTo(Memory);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in ProcessDecryptedData.");
        }
    }

    public virtual void Send<T>(T obj) where T : IPacketSerializable
    {
        // Serialize the object into a packet
        var packet = PacketSerializer.Serialize(obj);
        Send(ref packet);
    }

    /// <summary>
    /// Sends a packet using the SslStream.
    /// </summary>
    /// <param name="packet">The packet to send.</param>
    public virtual void Send(ref Packet packet)
    {
        if (!Connected) return;

        // Convert the packet to a byte array
        var data = packet.ToArray();

        // Write the packet data to the SslStream
        SslStream.Write(data, 0, data.Length);
    }

    /// <inheritdoc />
    public virtual void Disconnect()
    {
        if (!Connected)
            return;

        Connected = false;

        try
        {
            Socket.Shutdown(SocketShutdown.Receive);
        }
        catch
        {
            //ignored
        }

        try
        {
            OnDisconnected?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            //ignored
        }

        Dispose();
    }

    public virtual void Close() => Socket.Close();

    #endregion
}

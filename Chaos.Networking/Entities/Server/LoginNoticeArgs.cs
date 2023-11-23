using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Entities.Server;

/// <summary>
///     Represents the serialization of the <see cref="Chaos.Packets.Abstractions.Definitions.ServerOpCode.LoginNotice" />
///     packet
/// </summary>
public sealed record LoginNoticeArgs : ISendArgs
{
    /// <summary>
    ///     The checksum of the notice
    /// </summary>
    public uint? CheckSum { get; set; }
    /// <summary>
    ///     The raw data of the notice
    /// </summary>
    public byte[]? Data { get; set; }
    /// <summary>
    ///     Whether or not this response also contains the full data of the login notice
    /// </summary>
    public bool IsFullResponse { get; set; }
}
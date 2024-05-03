using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Entities.Server;

/// <summary>
///     Represents the serialization of the <see cref="Chaos.Packets.Abstractions.Definitions.ServerOpCode.MapData" />
///     packet
/// </summary>
public sealed record MapDataArgs : ISendArgs
{
    /// <summary>
    ///     Map data is sent in chunks, 1 row at a time. This is the current Y coordinate being sent
    /// </summary>
    public byte CurrentYIndex { get; set; }
    /// <summary>
    ///     The raw map data for the current row
    /// </summary>
    public byte[] MapData { get; set; } = [];
    /// <summary>
    ///     The width of the row being sent
    /// </summary>
    public byte Width { get; set; }
}
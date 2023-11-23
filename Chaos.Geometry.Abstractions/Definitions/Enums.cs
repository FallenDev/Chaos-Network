namespace Chaos.Geometry.Abstractions.Definitions;

/// <summary>
///     Defines the possible directions an object can face, and their networking byte values
/// </summary>
public enum Direction : byte
{
    /// <summary>
    ///     Up arrow key
    /// </summary>
    Up = 0,
    /// <summary>
    ///     Right arrow key
    /// </summary>
    Right = 1,
    /// <summary>
    ///     Down arrow key
    /// </summary>
    Down = 2,
    /// <summary>
    ///     Left arrow key
    /// </summary>
    Left = 3
}
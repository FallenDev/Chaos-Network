using System.Diagnostics.CodeAnalysis;
using System.Text;

using Chaos.Extensions.Common;
using Chaos.Packets;
using Chaos.Packets.Abstractions;

using Microsoft.Extensions.DependencyInjection;

namespace Chaos.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection helpers for registering packet serialization.
/// </summary>
[ExcludeFromCodeCoverage]
public static class PacketExtensions
{
    /// <summary>
    /// Registers <see cref="PacketSerializer"/> and all <see cref="IPacketConverter"/> implementations
    /// using reflection-based discovery.
    /// </summary>
    public static void AddPacketSerializer(this IServiceCollection services)
    {
        services.AddSingleton<IPacketSerializer>(sp =>
        {
            var converters = LoadConverters(sp);
            return new PacketSerializer(converters, Encoding.GetEncoding(949));
        });
    }

    private static IReadOnlyList<IPacketConverter> LoadConverters(IServiceProvider sp)
    {
        // Load once and materialize
        var converterTypes = typeof(IPacketConverter)
            .LoadImplementations()
            .ToArray();

        var list = new List<IPacketConverter>(converterTypes.Length);

        foreach (var type in converterTypes)
        {
            var instance = (IPacketConverter)ActivatorUtilities.CreateInstance(sp, type);
            list.Add(instance);
        }

        return list;
    }
}

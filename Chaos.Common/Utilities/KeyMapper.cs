using System.Collections.Concurrent;
using System.Numerics;
using Chaos.Common.Abstractions;

namespace Chaos.Common.Utilities;

/// <summary>
///     A utility class for mapping string keys to numeric ids
/// </summary>
/// <typeparam name="T">The numeric type to map the keys to</typeparam>
public class KeyMapper<T> where T: INumber<T>
{
    /// <summary>
    ///     The id generator used to generate ids for keys
    /// </summary>
    protected IIdGenerator<T> IdGenerator { get; }
    /// <summary>
    ///     The map of keys to ids
    /// </summary>
    protected ConcurrentDictionary<string, T> Map { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KeyMapper{T}" /> class.
    /// </summary>
    /// <param name="idGenerator">The id generator to use</param>
    public KeyMapper(IIdGenerator<T> idGenerator)
    {
        IdGenerator = idGenerator;
        Map = new ConcurrentDictionary<string, T>();
    }

    /// <summary>
    ///     Maps the specified key to a unique id. If the key is already mapped, the existing id is returned.
    /// </summary>
    /// <param name="key">The key to map</param>
    public T GetId(string key) => Map.GetOrAdd(key, static (_, idGenerator) => idGenerator.NextId, IdGenerator);
}
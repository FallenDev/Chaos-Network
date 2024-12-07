using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Chaos.Extensions.Common;

/// <summary>
///     Provides extension methods for <see cref="System.Collections.Generic.IEnumerable{T}" />
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    ///     Determines if a sequence of strings contains a specific strings in a case insensitive manner.
    /// </summary>
    public static bool ContainsI(this IEnumerable<string> enumerable, string str)
        => enumerable.Contains(str, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Finds the next highest number in a sequence from a given value
    /// </summary>
    /// <param name="enumerable">
    ///     The sequence to search
    /// </param>
    /// <param name="seed">
    ///     The starting value
    /// </param>
    /// <typeparam name="T">
    ///     A numeric type
    /// </typeparam>
    public static T NextHighest<T>(this IEnumerable<T> enumerable, T seed) where T: INumber<T>
    {
        var current = seed;

        foreach (var number in enumerable)
        {
            //dont consider any numbers that are less than or equal to the seed
            if (number <= seed)
                continue;

            //if the current number is the seed, take the first number that reaches this statement
            //only numbers that are greater than the seed will reach this statement
            if (current == seed)
                current = number;

            //otherwise, if the number is less than the current number, take it
            //all numbers that reach this statement are greater than the seed
            else if (number < current)
                current = number;
        }

        return current;
    }

    /// <summary>
    ///     Finds the next lowest number in a sequence from a given value
    /// </summary>
    /// <param name="enumerable">
    ///     The sequence to search
    /// </param>
    /// <param name="seed">
    ///     The starting value
    /// </param>
    /// <typeparam name="T">
    ///     A numeric type
    /// </typeparam>
    public static T NextLowest<T>(this IEnumerable<T> enumerable, T seed) where T: INumber<T>
    {
        var current = seed;

        foreach (var number in enumerable)
        {
            //dont consider any numbers that are greater than or equal to the seed
            if (number >= seed)
                continue;

            //if the current number is the seed, take the first number that reaches this statement
            //only numbers that are less than the seed will reach this statement
            if (current == seed)
                current = number;

            //otherwise, if the number is greater than the current number, take it
            //all numbers that reach this statement are lower than the seed
            else if (number > current)
                current = number;
        }

        return current;
    }

    /// <summary>
    ///     Orders the given enumerable by the given comparer
    /// </summary>
    public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> enumerable, IComparer<T> comparer)
        => enumerable.OrderBy(e => e, comparer);

    /// <summary>
    ///     Orders the given enumerable by the given comparer
    /// </summary>
    public static IOrderedEnumerable<T> OrderByDescending<T>(this IEnumerable<T> enumerable, IComparer<T> comparer)
        => enumerable.OrderByDescending(e => e, comparer);

    /// <summary>
    ///     Randomizes the order of the elements in a sequence
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Impossible to test randomness without creating an occasionally failing test")]
    public static List<T> Shuffle<T>(this IEnumerable<T> objects)
    {
        var list = objects.ToList();
        list.ShuffleInPlace();

        return list;
    }

    /// <summary>
    ///     Orders the given enumerable by the given comparer
    /// </summary>
    public static IOrderedEnumerable<T> ThenBy<T>(this IOrderedEnumerable<T> orderedEnumerable, IComparer<T> comparer)
        => orderedEnumerable.ThenBy(e => e, comparer);

    /// <summary>
    ///     Orders the given enumerable by the given comparer
    /// </summary>
    public static IOrderedEnumerable<T> ThenByDescending<T>(this IOrderedEnumerable<T> orderedEnumerable, IComparer<T> comparer)
        => orderedEnumerable.ThenByDescending(e => e, comparer);
}
using System.Collections.Concurrent;
using AzureKeyVaultEmulator.Shared.Exceptions;

namespace AzureKeyVaultEmulator.Shared.Utilities;

public static class DictionaryUtils
{
    /// <summary>
    /// Retrieves an item from <paramref name="dict"/>, handling null or missing items.
    /// </summary>
    /// <typeparam name="T">The type for the <paramref name="dict"/> value.</typeparam>
    /// <param name="dict">The dictionary to query.</param>
    /// <param name="name">The lookup key.</param>
    /// <returns>The value associated with <paramref name="name"/> of type <typeparamref name="T"/>.</returns>
    /// <exception cref="MissingItemException">Thrown when the value for <paramref name="name"/> is null.</exception>
    public static T SafeGet<T>(this ConcurrentDictionary<string, T> dict, string name)
        where T : notnull
    {
        var exists = dict.TryGetValue(name, out T? value);

        if (!exists || value is null)
            throw new MissingItemException($"Could not find {name} in vault.");

        return value;
    }

    /// <summary>
    /// Syntactic sugar for an annoying extension method on a Dictionary.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dict"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public static void SafeAddOrUpdate<T>(this ConcurrentDictionary<string, T> dict, string name, T value)
        => dict.AddOrUpdate(name, value, (_, _) => value);

    /// <summary>
    /// Removes the item for key <paramref name="key"/> from <paramref name="dict"/> without throwing exceptions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    public static void SafeRemove<T>(this ConcurrentDictionary<string, T> dict, string key)
        => dict.TryRemove(key, out _);
}

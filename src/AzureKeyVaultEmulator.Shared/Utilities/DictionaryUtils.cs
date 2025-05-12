using System.Collections.Concurrent;
using AzureKeyVaultEmulator.Shared.Exceptions;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

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
            throw new MissingItemException(name);

        return value;
    }

    public static async Task<TEntity> SafeGetAsync<TEntity>(
        this DbSet<TEntity> set,
        string name,
        string version = "",
        bool deleted = false)
    where TEntity : class, INamedItem, IDeletable
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var query = set.Where(x => x.PersistedName == name && x.Deleted == deleted);

        if (!string.IsNullOrEmpty(version))
            query = query.Where(x => x.PersistedVersion == version);

        var item = await query.FirstOrDefaultAsync();

        return item ?? throw new MissingItemException(name);
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

    public static async Task SafeAddAsync<TEntity>(
       this DbSet<TEntity> set,
       string name,
       string version,
       TEntity value)
    where TEntity : class, INamedItem
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);

        var existing = await set.FirstOrDefaultAsync(x => x.PersistedName == name && x.PersistedVersion == version);

        if (existing != null)
            return;

        value.PersistedName = name;
        value.PersistedVersion = version;

        set.Add(value);
    }

    /// <summary>
    /// Removes the item for key <paramref name="key"/> from <paramref name="dict"/> without throwing exceptions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    public static void SafeRemove<T>(this ConcurrentDictionary<string, T> dict, string key)
        => dict.TryRemove(key, out _);

    public static async Task SafeRemoveAsync<TEntity>(this DbSet<TEntity> set, string name, bool deleted = false)
        where TEntity : class, INamedItem
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var range = await set.Where(x => x.PersistedName == name && x.Deleted == deleted).ToListAsync();

        if (range == null || range.Count == 0)
            throw new MissingItemException($"Cannot find objects with the name {name} in the vault.");

        set.RemoveRange(range);
    }
}

using System.Collections.Concurrent;

namespace AzureKeyVaultEmulator.Shared.Utilities;

public static class DictionaryUtils
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dict"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static T SafeGet<T>(this ConcurrentDictionary<string, T> dict, string name)
        where T : notnull
    {
        var exists = dict.TryGetValue(name, out T? value);

        if (!exists || value is null)
            throw new ArgumentException($"Could not find {name} in vault.");

        return value;
    }

    public static void SafeAddOrUpdate<T>(this ConcurrentDictionary<string, T> dict, string name, T value)
        => dict.AddOrUpdate(name, value, (_, _) => value);

    public static void SafeRemove<T>(this ConcurrentDictionary<string, T> dict, string key)
        => dict.TryRemove(key, out _);
}

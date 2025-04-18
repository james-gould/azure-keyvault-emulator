using System.Collections.Concurrent;

namespace AzureKeyVaultEmulator.Shared.Utilities;

public static class DictionaryUtils
{
    public static T SafeGet<T>(this ConcurrentDictionary<string, T> dict, string name)
    {
        var exists = dict.TryGetValue(name, out T? value);

        if (!exists || value == null)
            throw new ArgumentException($"Could not find {name} in vault.");

        return value;
    }

    public static void SafeAddOrUpdate<T>(this ConcurrentDictionary<string, T> dict, string name, T value)
        => dict.AddOrUpdate(name, value, (_, _) => value);

    public static void SafeRemove<T>(this ConcurrentDictionary<string, T> dict, string key)
        => dict.TryRemove(key, out _);
}

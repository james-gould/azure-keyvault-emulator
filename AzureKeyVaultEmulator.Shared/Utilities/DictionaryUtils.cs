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
}

using Azure.Security.KeyVault.Keys;

namespace Xunit;

public partial class Assert
{
    public static void KeysAreEqual(KeyVaultKey first,  KeyVaultKey second)
    {
        Equal(first.Name, second.Name);
        Equal(first.Key.KeyType, second.Key.KeyType);
        Equal(first.Properties.Version, second.Properties.Version);
        Equal(first.Properties.VaultUri, second.Properties.VaultUri);
    }

    public static void KeysNotEqual(KeyVaultKey first, KeyVaultKey second)
    {
        NotEqual(first.Properties.Id, second.Properties.Id);
        NotEqual(first.Properties.Version, second.Properties.Version);
    }

    public static void KeyHasTag(KeyVaultKey key, string tagName, string expectedValue)
    {
        NotEmpty(key.Properties?.Tags);

        string? outValue = string.Empty;

        var exists = key.Properties?.Tags.TryGetValue(tagName, out outValue);

        if (exists is not null && !exists.Value)
            Fail($"No value for name: {tagName} found");

        if (string.IsNullOrEmpty(outValue))
            Fail($"Value for {tagName} was null or empty");

        Equal(expectedValue, outValue);
    }
}

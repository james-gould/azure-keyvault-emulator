using Azure.Security.KeyVault.Keys;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

public sealed class KeysTestingFixture : EmulatorTestingFixture
{
    private KeyClient? _client;

    public const string DefaultKeyName = "algKey";
    private KeyType _defaultType = KeyType.Rsa;

    public async ValueTask<KeyClient> GetKeyClientAsync()
    {
        if (_client is not null)
            return _client;

        var setup = await GetClientSetupModelAsync();

        var opt = new KeyClientOptions
        {
            DisableChallengeResourceVerification = true
        };

         return _client = new KeyClient(setup.VaultUri, setup.Credential, opt);
    }

    public async Task<KeyVaultKey> CreateKeyAsync(string name = DefaultKeyName, KeyType? type = null)
    {
        var client = await GetKeyClientAsync();

        type ??= _defaultType;

        var result = await client.CreateKeyAsync(name, type.Value);

        if(result?.Value is null)
            throw new InvalidOperationException($"Failed to create Key in {nameof(KeysTestingFixture)}");

        return result.Value;
    }
}

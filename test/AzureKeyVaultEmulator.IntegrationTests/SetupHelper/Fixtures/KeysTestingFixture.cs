using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

public sealed class KeysTestingFixture : KeyVaultClientTestingFixture<KeyClient>
{
    private KeyClient? _client;

    public const string DefaultKeyName = "algKey";
    private KeyType _defaultType = KeyType.Rsa;

    public override async ValueTask<KeyClient> GetClientAsync()
    {
        if (_client is not null)
            return _client;

        var setup = await GetClientSetupModelAsync();

        var opt = new KeyClientOptions
        {
            DisableChallengeResourceVerification = true,
            RetryPolicy = _clientRetryPolicy
        };

         return _client = new KeyClient(setup.VaultUri, setup.Credential, opt);
    }

    public async Task<CryptographyClient> GetCryptographyClientAsync(KeyVaultKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var bearer = await GetBearerTokenAsync();

        var cred = new EmulatedTokenCredential(bearer);

        return new CryptographyClient(key.Id, cred);
    }

    public async Task<KeyVaultKey> CreateKeyAsync(string name = DefaultKeyName, KeyType? type = null)
    {
        var client = await GetClientAsync();

        type ??= _defaultType;

        var result = await client.CreateKeyAsync(name, type.Value);

        if(result?.Value is null)
            throw new InvalidOperationException($"Failed to create Key in {nameof(KeysTestingFixture)}");

        return result.Value;
    }
}

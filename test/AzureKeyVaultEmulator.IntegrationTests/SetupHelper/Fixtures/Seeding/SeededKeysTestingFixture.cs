using Azure.Security.KeyVault.Keys;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures.Seeding;

public sealed class SeededKeysTestingFixture : SeedingTestingFixture<KeyClient>
{
    /// <summary>
    /// Mirrors the type seeded in <c>AzureKeyVaultEmulator.AppHost.Program</c> when the
    /// <c>--seeding</c> flag is supplied to the AppHost.
    /// </summary>
    public static readonly KeyType SeededKeyType = KeyType.Rsa;

    private KeyClient? _keyClient;

    public override async ValueTask<KeyClient> GetClientAsync()
    {
        if (_keyClient is not null)
            return _keyClient;

        var setup = await GetClientSetupModelAsync();

        var options = new KeyClientOptions
        {
            DisableChallengeResourceVerification = true,
            RetryPolicy = _clientRetryPolicy
        };

        return _keyClient = new KeyClient(setup.VaultUri, setup.Credential, options);
    }
}

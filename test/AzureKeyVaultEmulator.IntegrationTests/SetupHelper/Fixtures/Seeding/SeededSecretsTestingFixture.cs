using Azure.Security.KeyVault.Secrets;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures.Seeding;

public sealed class SeededSecretsTestingFixture : SeedingTestingFixture<SecretClient>
{
    private SecretClient? _secretClient;

    public override async ValueTask<SecretClient> GetClientAsync()
    {
        if (_secretClient is not null)
            return _secretClient;

        var setup = await GetClientSetupModelAsync();

        var options = new SecretClientOptions
        {
            DisableChallengeResourceVerification = true,
            RetryPolicy = _clientRetryPolicy
        };

        return _secretClient = new SecretClient(setup.VaultUri, setup.Credential, options);
    }
}

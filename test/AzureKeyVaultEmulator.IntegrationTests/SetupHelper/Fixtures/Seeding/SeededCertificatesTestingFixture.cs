using Azure.Security.KeyVault.Certificates;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures.Seeding;

public sealed class SeededCertificatesTestingFixture : SeedingTestingFixture<CertificateClient>
{
    private CertificateClient? _certClient;

    public override async ValueTask<CertificateClient> GetClientAsync()
    {
        if (_certClient is not null)
            return _certClient;

        var setup = await GetClientSetupModelAsync();

        var options = new CertificateClientOptions
        {
            DisableChallengeResourceVerification = true,
            RetryPolicy = _clientRetryPolicy
        };

        return _certClient = new CertificateClient(setup.VaultUri, setup.Credential, options);
    }
}

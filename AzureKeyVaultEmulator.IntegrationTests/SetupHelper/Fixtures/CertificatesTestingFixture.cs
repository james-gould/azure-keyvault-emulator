using Azure.Security.KeyVault.Certificates;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

public sealed class CertificatesTestingFixture : KeyVaultClientTestingFixture<CertificateClient>
{
    private CertificateClient? _certClient;

    public CertificatePolicy BasicPolicy = CertificatePolicy.Default;

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

    public async Task<KeyVaultCertificateWithPolicy>
        CreateCertificateAsync(string name, string? password = null)
    {
        var client = await GetClientAsync();

        var createdCertificate = await client.StartCreateCertificateAsync(name, CertificatePolicy.Default);

        return await client.GetCertificateAsync(name);

        //return X509CertificateLoader.LoadCertificate(cert.Value.Cer);
    }
}

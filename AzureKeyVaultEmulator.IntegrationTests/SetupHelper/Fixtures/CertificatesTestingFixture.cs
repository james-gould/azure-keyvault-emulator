using System.Security.Cryptography.X509Certificates;
using Azure.Security.KeyVault.Certificates;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

public sealed class CertificatesTestingFixture : KeyVaultClientTestingFixture<CertificateClient>
{
    private CertificateClient? _certClient;

    public CertificatePolicy BasicPolicy
        = new() { KeySize = 2048, ContentType = CertificateContentType.Pkcs12 };

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

    public async Task<X509Certificate2> CreateCertificateAsync(string name, string? password = null)
    {
        var client = await GetClientAsync();

        var createdCertificate = (await client.StartCreateCertificateAsync(name, CertificatePolicy.Default)).Value;

        return X509CertificateLoader.LoadCertificate(createdCertificate.Cer);
    }
}

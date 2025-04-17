using System.Security.Cryptography.X509Certificates;
using Azure.Security.KeyVault.Certificates;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

public sealed class CertificatesTestingFixture : KeyVaultClientTestingFixture<CertificateClient>
{
    private CertificateClient? _certClient;

    public override async ValueTask<CertificateClient> GetClientAsync()
    {
        if (_certClient is not null)
            return _certClient;

        var setup = await GetClientSetupModelAsync();

        return _certClient = new CertificateClient(setup.VaultUri, setup.Credential);
    }

    public async ValueTask<X509Certificate2> CreateCertificateAsync(string name, string? password = null)
    {
        if (string.IsNullOrEmpty(name))
            name = FreshlyGeneratedGuid;

        return X509CertificateLoader.
    }
}

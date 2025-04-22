using Azure.Security.KeyVault.Certificates;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

public sealed class CertificatesTestingFixture : KeyVaultClientTestingFixture<CertificateClient>
{
    private CertificateClient? _certClient;

    public CertificatePolicy BasicPolicy = CertificatePolicy.Default;

    public AdministratorContact DefaultAdminContact = new()
    {
        Email = "emulator@keyvault.net",
        FirstName = "Azure",
        LastName = "Key Vault",
        Phone = "0118 999 881 999 119 7253"
    };

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

        await client.StartCreateCertificateAsync(name, CertificatePolicy.Default);

        var cert = await client.GetCertificateAsync(name);

        return cert.Value;
    }

    public CertificateIssuer CreateIssuerConfiguration(string issuerName, string provider = "Self")
    {
        var issuerConfig = new CertificateIssuer(issuerName, provider)
        {
            AccountId = FreshlyGeneratedGuid,
            Password = FreshlyGeneratedGuid,
            Enabled = true,
            OrganizationId = FreshlyGeneratedGuid
        };

        issuerConfig.AdministratorContacts.Add(DefaultAdminContact);

        return issuerConfig;
    }

    public string X509CertificateWithPrivateKey = "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";
}

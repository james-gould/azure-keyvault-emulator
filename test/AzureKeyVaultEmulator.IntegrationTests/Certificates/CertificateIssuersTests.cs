using Azure.Security.KeyVault.Certificates;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;
using AzureKeyVaultEmulator.Shared.Constants;

namespace AzureKeyVaultEmulator.IntegrationTests.Certificates;

public class CertificateIssuersTests(CertificatesTestingFixture fixture) : IClassFixture<CertificatesTestingFixture>
{
    [Fact]
    public async Task DeletingIssuerWillRemoveFromStore()
    {
        var client = await fixture.GetClientAsync();

        var (issuerName, issuer) = await CreateIssuerAsync();

        var fromStore = await client.GetIssuerAsync(issuerName);

        Assert.NotNull(fromStore.Value);

        var deleteResponse = await client.DeleteIssuerAsync(issuerName);

        Assert.NotNull(deleteResponse.Value);

        Assert.Equal(deleteResponse.Value.Name, issuer.Name);

        await Assert.RequestFailsAsync(() => client.GetIssuerAsync(issuerName));
    }

    [Fact]
    public async Task UpdatingIssuerBundlePersistsChangeInStore()
    {
        var client = await fixture.GetClientAsync();

        var (issuerName, issuer) = await CreateIssuerAsync();

        var fromStore = await client.GetIssuerAsync(issuerName);

        Assert.Equivalent(issuer, fromStore.Value);

        var issuerToBeUpdated = fromStore.Value;

        var adminContact = issuerToBeUpdated.AdministratorContacts.FirstOrDefault();

        Assert.NotNull(adminContact);

        var newEmail = "testing@integrationtest.com";

        adminContact!.Email = newEmail;

        var response = await client.UpdateIssuerAsync(issuerToBeUpdated);

        Assert.NotNull(response.Value);

        var updatedEmail = response.Value.AdministratorContacts?.FirstOrDefault()?.Email;

        Assert.NotEqual(fixture.DefaultAdminContact.Email, updatedEmail);

        Assert.Equal(newEmail, updatedEmail);
    }

    [Fact]
    public async Task GetCertificateIssuerWillReturnFromStore()
    {
        var client = await fixture.GetClientAsync();

        var issuerName = fixture.FreshlyGeneratedGuid;
        var certName = fixture.FreshlyGeneratedGuid;

        await Assert.RequestFailsAsync(() => client.GetIssuerAsync(issuerName));

        var issuerConfig = fixture.CreateIssuerConfiguration(issuerName);

        var createdResponse = await client.CreateIssuerAsync(issuerConfig);

        var response = await client.GetIssuerAsync(issuerName);

        Assert.NotNull(response.Value);

        var issuer = response.Value;

        Assert.IssuersAreEqual(issuerConfig, issuer);
    }

    

    private async Task<(string issuerName, CertificateIssuer issuer)> CreateIssuerAsync()
    {
        var client = await fixture.GetClientAsync();

        var issuerName = fixture.FreshlyGeneratedGuid;

        var config = fixture.CreateIssuerConfiguration(issuerName);

        var response = await client.CreateIssuerAsync(config);

        Assert.NotNull(response.Value);

        var issuer = response.Value;

        return (issuerName, issuer);
    }
    [Fact]
    public async Task UpdatingCertificateToUseIssuerPersistsChangeInStore()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;
        var certificateResponse = await fixture.CreateCertificateAsync(certName);

        Assert.NotNull(certificateResponse);
        Assert.Equal(certName, certificateResponse.Name);

        var (issuerName, issuer) = await CreateIssuerAsync();
        var issuerFromStore = await client.GetIssuerAsync(issuerName);

        Assert.NotNull(issuerFromStore.Value);
        Assert.Equal(issuerName, issuerFromStore.Value.Name);
        Assert.NotEqual(issuerName, certificateResponse.Policy.IssuerName);

        var certificatePolicy = new CertificatePolicy(issuerName, AuthConstants.EmulatorIss)
        {
            KeySize = 3096,
            Enabled = false,
            ValidityInMonths = 12,
            CertificateTransparency = false,
            Exportable = false
        };

        var updatedCertificateResponse = await client.UpdateCertificatePolicyAsync(certName, certificatePolicy);

        Assert.NotNull(updatedCertificateResponse.Value);

        var updatedPolicy = updatedCertificateResponse.Value;

        Assert.Equal(issuerName, updatedPolicy.IssuerName);

        var updatedCertificate = await client.GetCertificateAsync(certName);

        Assert.NotNull(updatedCertificate.Value.Policy);

        var policy = updatedCertificate.Value.Policy;

        Assert.Equivalent(policy, updatedPolicy);
        Assert.Equal(issuerName, updatedCertificate.Value.Policy.IssuerName);
    }
}

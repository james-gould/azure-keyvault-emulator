using Azure.Security.KeyVault.Certificates;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

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
}

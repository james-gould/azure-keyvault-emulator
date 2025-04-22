using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

namespace AzureKeyVaultEmulator.IntegrationTests.Certificates;

public class CertificateIssuersTests(CertificatesTestingFixture fixture) : IClassFixture<CertificatesTestingFixture>
{
    [Fact]
    public async Task DeletingIssuerWillRemoveFromStore()
    {
        var client = await fixture.GetClientAsync();

        var issuerName = fixture.FreshlyGeneratedGuid;

        var issuerConfig = fixture.CreateIssuerConfiguration(issuerName);

        var response = await client.CreateIssuerAsync(issuerConfig);

        Assert.NotNull(response.Value);

        var issuer = response.Value;

        var fromStore = await client.GetIssuerAsync(issuerName);

        Assert.NotNull(fromStore.Value);

        var deleteResponse = await client.DeleteIssuerAsync(issuerName);

        Assert.NotNull(deleteResponse.Value);

        Assert.Equal(deleteResponse.Value.Name, issuer.Name);

        await Assert.RequestFailsAsync(() => client.GetIssuerAsync(issuerName));
    }
}

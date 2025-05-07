using Azure.Security.KeyVault.Certificates;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

namespace AzureKeyVaultEmulator.IntegrationTests.Certificates;

public class CertificateContactTests(CertificatesTestingFixture fixture) : IClassFixture<CertificatesTestingFixture>
{
    [Fact]
    public async Task SetCertificateContactsWillPersistInStore()
    {
        var client = await fixture.GetClientAsync();

        var contacts = await SetContactsAsync();

        var fromStore = await client.GetContactsAsync();

        Assert.Equivalent(Contacts, fromStore?.Value);
    }

    [Fact]
    public async Task DeleteCertificateContactsWillRemoveFromstore()
    {
        var client = await fixture.GetClientAsync();

        var contacts = await SetContactsAsync();

        var deleteResponse = await client.DeleteContactsAsync();

        Assert.NotNull(deleteResponse.Value);

        var getResponse = await client.GetContactsAsync();

        Assert.NotNull(getResponse.Value);

        var removedContacts = getResponse.Value;

        Assert.NotEqual(contacts, removedContacts);

        Assert.Empty(removedContacts);
    }

    private async Task<IEnumerable<CertificateContact>> SetContactsAsync()
    {
        var client = await fixture.GetClientAsync();

        var response = await client.SetContactsAsync(Contacts);

        Assert.NotNull(response.Value);

        Assert.Equivalent(response.Value, Contacts);

        return response.Value;
    }

    private List<CertificateContact> Contacts
        => [ new() { Email = "test@test.com", Name = "myName", Phone = "+00 000 000 000" } ];

}

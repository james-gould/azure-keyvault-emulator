using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;
using Azure.Security.KeyVault.Certificates;
using AzureKeyVaultEmulator.IntegrationTests.Extensions;

namespace AzureKeyVaultEmulator.IntegrationTests.Certificates;

public class CertificatesControllerTests(CertificatesTestingFixture fixture)
    : IClassFixture<CertificatesTestingFixture>
{
    [Fact]
    public async Task EvaulatingCertificateOperationWillSucceed()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        await Assert.ThrowsRequestFailedAsync(() => client.GetCertificateAsync(certName));

        var policy = new CertificatePolicy
        {
            ContentType = CertificateContentType.Pkcs12,
            KeySize = 2048
        };

        var operation = await client.StartCreateCertificateAsync(certName, policy, enabled: true);

        Assert.NotNull(operation);

        await operation.UpdateStatusAsync();

        var certificateFromStore = await client.GetCertAsync(certName);

        Assert.Equal(certName, certificateFromStore.Name);
    }
}

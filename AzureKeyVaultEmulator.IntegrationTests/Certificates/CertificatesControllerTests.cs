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

        var operation = await client.StartCreateCertificateAsync(certName, new CertificatePolicy
        {
            ContentType = CertificateContentType.Pkcs12,
            KeySize = 2048
        });

        Assert.NotNull(operation);

        var certificateResult = await operation.WaitForCompletionAsync();

        Assert.NotNull(certificateResult.Value);

        var certificateFromStore = await client.GetCertAsync(certName);

        Assert.CertificatesAreEqual(certificateFromStore, certificateResult.Value);
    }
}

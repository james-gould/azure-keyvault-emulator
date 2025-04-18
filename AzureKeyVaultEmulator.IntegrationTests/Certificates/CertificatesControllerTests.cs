using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;
using AzureKeyVaultEmulator.IntegrationTests.Extensions;

namespace AzureKeyVaultEmulator.IntegrationTests.Certificates;

public class CertificatesControllerTests(CertificatesTestingFixture fixture)
    : IClassFixture<CertificatesTestingFixture>
{
    [Fact]
    public async Task NotWaitingForOperationWillThrow()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        await Assert.ThrowsRequestFailedAsync(() => client.GetCertAsync(certName));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var cert = (await client.StartCreateCertificateAsync(certName, fixture.BasicPolicy, enabled: true)).Value;
        });
    }

    [Fact]
    public async Task EvaulatingCertificateOperationWillSucceed()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        await Assert.ThrowsRequestFailedAsync(() => client.GetCertificateAsync(certName));

        var operation = await client.StartCreateCertificateAsync(certName, fixture.BasicPolicy, enabled: true);

        Assert.NotNull(operation);

        var response = await operation.UpdateStatusAsync();

        Assert.Equal((int)HttpStatusCode.OK, response.Status);

        var certificateFromStore = await client.GetCertAsync(certName);

        Assert.Equal(certName, certificateFromStore.Name);
    }

    [Fact]
    public async Task WaitForCompletionWillCompleteCreationOperation()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        await Assert.ThrowsRequestFailedAsync(() => client.GetCertificateAsync(certName));

        var operation = await client.StartCreateCertificateAsync(certName, fixture.BasicPolicy, enabled: true);

        await operation.WaitForCompletionAsync();

        Assert.True(operation.HasCompleted);

        var certificateFromStore = await client.GetCertAsync(certName);

        Assert.Equal(certName, certificateFromStore.Name);
    }
}

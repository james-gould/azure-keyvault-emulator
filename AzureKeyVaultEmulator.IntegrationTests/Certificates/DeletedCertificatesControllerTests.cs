using Azure.Security.KeyVault.Certificates;
using AzureKeyVaultEmulator.IntegrationTests.Extensions;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

namespace AzureKeyVaultEmulator.IntegrationTests.Certificates;
public class DeletedCertificatesControllerTests(CertificatesTestingFixture fixture) : IClassFixture<CertificatesTestingFixture>
{
    [Fact]
    public async Task GetDeletedCertWillServeFromDeletedStore()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var executionCount = await RequestSetup
            .CreateMultiple(26, 51, i => fixture.CreateCertificateAsync(certName));

        var deleteOp = await client.StartDeleteCertificateAsync(certName);

        await deleteOp.WaitForCompletionAsync();

        Assert.NotNull(deleteOp.Value);

        var deletedCert = await client.GetDeletedCertificateAsync(certName);

        Assert.NotNull(deletedCert.Value);

        Assert.Equal(certName, deletedCert.Value.Name);
    }

    [Fact]
    public async Task GetDeletedCertificatesWillCycleLink()
    {
        var client = await fixture.GetClientAsync();

        var name = fixture.FreshlyGeneratedGuid;

        var executionCount = await RequestSetup
            .CreateMultiple(26, 51, i => fixture.CreateCertificateAsync(name));

        var deleteOp = await client.StartDeleteCertificateAsync(name);

        await deleteOp.WaitForCompletionAsync();

        Assert.NotNull(deleteOp.Value);

        List<DeletedCertificate> deletedCerts = [];

        await foreach (var certificate in client.GetDeletedCertificatesAsync())
            if(certificate.Name.Contains(name))
                deletedCerts.Add(certificate);

        // All versions are deleted with just one preserved.
        // When restoring only one version, the latest, should be restored
        Assert.Single(deletedCerts);
    }

    [Fact]
    public async Task RecoveredCertificateMovedBackToMainStore()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var cert = await fixture.CreateCertificateAsync(certName);

        var beforeDelete = await client.GetCertificateAsync(certName);

        Assert.Equal(certName, beforeDelete?.Value.Name);

        var deleteOp = await client.StartDeleteCertificateAsync(certName);

        await deleteOp.WaitForCompletionAsync();

        var afterDelete = await client.GetDeletedCertificateAsync(certName);

        Assert.Equal(certName, afterDelete?.Value?.Name);

        var recoveryOp = await client.StartRecoverDeletedCertificateAsync(certName);

        await recoveryOp.WaitForCompletionAsync();

        var afterRecovery = await client.GetCertAsync(certName);

        Assert.CertificatesAreEqual(cert, afterRecovery);

        await Assert.ThrowsRequestFailedAsync(() => client.GetDeletedCertificateAsync(certName));
    }
}

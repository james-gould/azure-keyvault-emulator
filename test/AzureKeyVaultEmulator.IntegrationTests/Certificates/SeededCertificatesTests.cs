using AzureKeyVaultEmulator.IntegrationTests.Extensions;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures.Seeding;
using AzureKeyVaultEmulator.Shared.Constants.Orchestration;

namespace AzureKeyVaultEmulator.IntegrationTests.Certificates;

public sealed class SeededCertificatesTests(SeededCertificatesTestingFixture fixture)
    : IClassFixture<SeededCertificatesTestingFixture>
{
    [Fact]
    public async Task SeededCertificateIsAvailableInRunningEmulatorTest()
    {
        var client = await fixture.GetClientAsync();

        var seededCert = await client.GetCertAsync(SeedingConstants.SeededCertificateName);

        Assert.NotNull(seededCert);
        Assert.Equal(SeedingConstants.SeededCertificateName, seededCert.Name);
    }

    [Fact]
    public async Task SeededCertificateHasVersionTest()
    {
        var client = await fixture.GetClientAsync();

        var seededCert = await client.GetCertAsync(SeedingConstants.SeededCertificateName);

        Assert.NotNull(seededCert.Properties);
        Assert.False(string.IsNullOrEmpty(seededCert.Properties.Version));
    }

    [Fact]
    public async Task SeededCertificateWillHaveAttributesSetTest()
    {
        var client = await fixture.GetClientAsync();

        var seededCert = await client.GetCertAsync(SeedingConstants.SeededCertificateName);

        Assert.NotNull(seededCert);
        Assert.NotNull(seededCert.Properties.RecoveryLevel);
        Assert.NotNull(seededCert.Properties.RecoverableDays);
    }
}

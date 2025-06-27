using Xunit;

namespace AzureKeyVaultEmulator.TestContainers.Tests;

/// <summary>
/// Tests for the AzureKeyVaultEmulatorConstants class.
/// </summary>
public class AzureKeyVaultEmulatorConstantsTests
{
    [Fact]
    public void Constants_HaveExpectedValues()
    {
        // Assert
        Assert.Equal("docker.io", AzureKeyVaultEmulatorConstants.Registry);
        Assert.Equal("jamesgoulddev/azure-keyvault-emulator", AzureKeyVaultEmulatorConstants.Image);
        Assert.Equal("latest", AzureKeyVaultEmulatorConstants.Tag);
        Assert.Equal(4997, AzureKeyVaultEmulatorConstants.Port);
        Assert.Equal("/certs", AzureKeyVaultEmulatorConstants.CertificatesMountPath);
        Assert.Equal("emulator.pfx", AzureKeyVaultEmulatorConstants.RequiredPfxFileName);
        Assert.Equal("Persist", AzureKeyVaultEmulatorConstants.PersistEnvironmentVariable);
    }
}
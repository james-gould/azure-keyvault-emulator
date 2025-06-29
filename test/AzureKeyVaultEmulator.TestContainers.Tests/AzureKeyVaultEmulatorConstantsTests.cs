using AzureKeyVaultEmulator.TestContainers.Constants;
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
        Assert.Equal("docker.io", AzureKeyVaultEmulatorContainerConstants.Registry);
        Assert.Equal("jamesgoulddev/azure-keyvault-emulator", AzureKeyVaultEmulatorContainerConstants.Image);
        Assert.Equal("latest", AzureKeyVaultEmulatorContainerConstants.Tag);
        Assert.Equal(4997, AzureKeyVaultEmulatorContainerConstants.Port);
        Assert.Equal("/certs", AzureKeyVaultEmulatorCertConstants.CertMountTarget);
        Assert.Equal("emulator.pfx", AzureKeyVaultEmulatorCertConstants.Pfx);
        Assert.Equal("Persist", AzureKeyVaultEmulatorContainerConstants.PersistData);
    }
}

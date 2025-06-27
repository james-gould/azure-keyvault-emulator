using Xunit;

namespace AzureKeyVaultEmulator.TestContainers.Tests;

/// <summary>
/// Integration tests for the AzureKeyVaultEmulatorContainer.
/// These tests demonstrate real usage patterns but require Docker to be available.
/// </summary>
public class AzureKeyVaultEmulatorContainerIntegrationTests
{
    [Fact(Skip = "Integration test - requires Docker")]
    public async Task Container_CanStartAndStop_Successfully()
    {
        // Arrange
        var tempDir = CreateTempDirectoryWithValidCertificates();

        try
        {
            await using var container = new AzureKeyVaultEmulatorContainer(tempDir, persist: false);

            // Act
            await container.StartAsync();

            // Assert
            var endpoint = container.GetConnectionString();
            Assert.StartsWith("https://", endpoint);
            Assert.Contains("4997", endpoint);

            // Verify we can get the mapped port
            var port = container.GetMappedPublicPort(AzureKeyVaultEmulatorConstants.Port);
            Assert.True(port > 0);

            // Stop the container
            await container.StopAsync();
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Fact(Skip = "Integration test - requires Docker")]
    public async Task Container_WithPersistence_ConfiguresCorrectly()
    {
        // Arrange
        var tempDir = CreateTempDirectoryWithValidCertificates();

        try
        {
            await using var container = new AzureKeyVaultEmulatorContainer(tempDir, persist: true);

            // Act
            await container.StartAsync();

            // Assert
            var endpoint = container.GetConnectionString();
            Assert.StartsWith("https://", endpoint);

            // Stop the container
            await container.StopAsync();
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// Creates a temporary directory with valid certificate files for testing.
    /// In a real scenario, these would be proper SSL certificates.
    /// </summary>
    /// <returns>The path to the temporary directory.</returns>
    private static string CreateTempDirectoryWithValidCertificates()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        // Create dummy certificate files for testing
        var pfxPath = Path.Combine(tempDir, AzureKeyVaultEmulatorConstants.RequiredPfxFileName);
        var crtPath = Path.Combine(tempDir, "emulator.crt");

        File.WriteAllText(pfxPath, "dummy pfx content");
        File.WriteAllText(crtPath, "dummy crt content");

        return tempDir;
    }
}
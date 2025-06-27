using Xunit;

namespace AzureKeyVaultEmulator.TestContainers.Tests;

/// <summary>
/// Tests for the AzureKeyVaultEmulatorContainer class.
/// </summary>
public class AzureKeyVaultEmulatorContainerTests
{
    [Fact]
    public void Constructor_WithNullCertificatesDirectory_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new AzureKeyVaultEmulatorContainer(null!));
        Assert.Equal("Certificates directory path cannot be null or empty. (Parameter 'certificatesDirectory')", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyCertificatesDirectory_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new AzureKeyVaultEmulatorContainer(""));
        Assert.Equal("Certificates directory path cannot be null or empty. (Parameter 'certificatesDirectory')", exception.Message);
    }

    [Fact]
    public void Constructor_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = "/non/existent/path";

        // Act & Assert
        var exception = Assert.Throws<DirectoryNotFoundException>(() => new AzureKeyVaultEmulatorContainer(nonExistentPath, persist: true, generateCertificates: false));
        Assert.Equal($"Certificates directory not found: {nonExistentPath}", exception.Message);
    }

    [Fact]
    public void Constructor_WithExistingDirectoryButMissingPfx_ThrowsFileNotFoundException()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act & Assert
            var exception = Assert.Throws<FileNotFoundException>(() => new AzureKeyVaultEmulatorContainer(tempDir, persist: true, generateCertificates: false));
            Assert.Contains("Required certificate file 'emulator.pfx' not found in directory:", exception.Message);
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Constructor_WithValidCertificatesDirectory_CreatesContainer()
    {
        // Arrange
        var tempDir = CreateTempDirectoryWithPfx();

        try
        {
            // Act
            using var container = new AzureKeyVaultEmulatorContainer(tempDir);

            // Assert - Just check that container was created without exception
            Assert.NotNull(container);
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetConnectionString_BeforeStart_ThrowsInvalidOperationException()
    {
        // Arrange
        var tempDir = CreateTempDirectoryWithPfx();

        try
        {
            using var container = new AzureKeyVaultEmulatorContainer(tempDir);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => container.GetConnectionString());
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetEndpoint_BeforeStart_ThrowsInvalidOperationException()
    {
        // Arrange
        var tempDir = CreateTempDirectoryWithPfx();

        try
        {
            using var container = new AzureKeyVaultEmulatorContainer(tempDir);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => container.GetEndpoint());
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Constructor_WithNonExistentDirectoryAndGenerateCertificates_CreatesDirectoryAndCertificates()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            // Act
            using var container = new AzureKeyVaultEmulatorContainer(tempDir, persist: true, generateCertificates: true);

            // Assert
            Assert.True(Directory.Exists(tempDir));
            Assert.True(File.Exists(Path.Combine(tempDir, AzureKeyVaultEmulatorConstants.RequiredPfxFileName)));
            Assert.True(File.Exists(Path.Combine(tempDir, "emulator.crt")));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Constructor_WithExistingDirectoryAndMissingCertificatesAndGenerateCertificates_CreatesCertificates()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            using var container = new AzureKeyVaultEmulatorContainer(tempDir, persist: true, generateCertificates: true);

            // Assert
            Assert.True(File.Exists(Path.Combine(tempDir, AzureKeyVaultEmulatorConstants.RequiredPfxFileName)));
            Assert.True(File.Exists(Path.Combine(tempDir, "emulator.crt")));
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// Creates a temporary directory with a dummy emulator.pfx file for testing.
    /// </summary>
    /// <returns>The path to the temporary directory.</returns>
    private static string CreateTempDirectoryWithPfx()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var pfxPath = Path.Combine(tempDir, AzureKeyVaultEmulatorConstants.RequiredPfxFileName);
        File.WriteAllText(pfxPath, "dummy content"); // Create a dummy file for testing

        return tempDir;
    }
}
# TestContainers Module Usage Examples

This directory contains examples of how to use the Azure KeyVault Emulator TestContainers module in different scenarios.

## Certificate Requirements

The TestContainers module requires a directory containing valid SSL certificates:

- `emulator.pfx` - Required PFX certificate file
- `emulator.crt` - Optional CRT certificate file

You can generate these certificates using the existing Azure KeyVault Emulator tools or by following the SSL certificate setup instructions outlined in the [setup.sh script](https://github.com/james-gould/azure-keyvault-emulator/blob/master/docs/setup.sh).

## Automatic Certificate Generation

The TestContainers module includes automatic certificate generation that is **enabled by default**. When you create a container, it will automatically generate the required SSL certificates if they don't already exist in the specified directory.

```csharp
// Certificates will be automatically generated if missing (default behavior)
await using var container = new AzureKeyVaultEmulatorContainer("/path/to/certs");

// Explicitly enable automatic certificate generation
await using var container = new AzureKeyVaultEmulatorContainer("/path/to/certs", generateCertificates: true);

// Disable automatic certificate generation (requires manual setup)
await using var container = new AzureKeyVaultEmulatorContainer("/path/to/certs", generateCertificates: false);
```

This feature eliminates the need for manual certificate setup in most testing scenarios, making it easier to get started with the emulator.

## CI/CD Usage

When using the TestContainers module in CI/CD environments like GitHub Actions, you can use the system's temporary directory for certificate storage. This ensures a clean environment for each run:

```csharp
using AzureKeyVaultEmulator.TestContainers;

// Use system temp directory for CI/CD environments
var certificatesPath = Path.GetTempPath();
await using var container = new AzureKeyVaultEmulatorContainer(certificatesPath);

// The container will automatically generate certificates in the temp directory
await container.StartAsync();
var endpoint = container.GetConnectionString();
```

On GitHub Actions, the temp directory is automatically cleaned between runs, making it ideal for ephemeral testing environments.

## Basic Usage

```csharp
using AzureKeyVaultEmulator.TestContainers;
using AzureKeyVaultEmulator.Aspire.Client;

// Create container with certificate directory (certificates will be auto-generated if missing)
var certificatesPath = "/path/to/certs"; // Directory will be created if it doesn't exist
await using var container = new AzureKeyVaultEmulatorContainer(certificatesPath);

// Start the container
await container.StartAsync();

// Get the endpoint
var endpoint = container.GetConnectionString();

// Use with the new helper methods for easier client creation
var secretClient = KeyVaultHelper.GetSecretClient(endpoint);
var keyClient = KeyVaultHelper.GetKeyClient(endpoint);
var certificateClient = KeyVaultHelper.GetCertificateClient(endpoint);

// Use the clients
await secretClient.SetSecretAsync("test-secret", "test-value");
var secret = await secretClient.GetSecretAsync("test-secret");

// Container will be automatically disposed when using statement ends
```

## XUnit Test Example

```csharp
using AzureKeyVaultEmulator.TestContainers;
using AzureKeyVaultEmulator.Aspire.Client;
using Azure.Security.KeyVault.Secrets;
using Xunit;

public class KeyVaultTests : IAsyncLifetime
{
    private AzureKeyVaultEmulatorContainer _container;
    private SecretClient _secretClient;

    public async Task InitializeAsync()
    {
        var certificatesPath = GetCertificatesPath();
        _container = new AzureKeyVaultEmulatorContainer(certificatesPath);
        await _container.StartAsync();

        var endpoint = _container.GetConnectionString();
        _secretClient = KeyVaultHelper.GetSecretClient(endpoint);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task CanStoreAndRetrieveSecret()
    {
        // Act
        await _secretClient.SetSecretAsync("test", "value");
        var secret = await _secretClient.GetSecretAsync("test");

        // Assert
        Assert.Equal("value", secret.Value.Value);
    }

    private string GetCertificatesPath()
    {
        // Return path to directory for certificates (will be auto-generated if missing)
        return Environment.GetEnvironmentVariable("KEYVAULT_CERTS_PATH") 
               ?? "/path/to/your/certificates";
    }
}
```

## NUnit Test Example

```csharp
using AzureKeyVaultEmulator.TestContainers;
using AzureKeyVaultEmulator.Aspire.Client;
using Azure.Security.KeyVault.Secrets;
using NUnit.Framework;

[TestFixture]
public class KeyVaultTests
{
    private AzureKeyVaultEmulatorContainer _container;
    private SecretClient _secretClient;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        var certificatesPath = GetCertificatesPath();
        _container = new AzureKeyVaultEmulatorContainer(certificatesPath);
        await _container.StartAsync();

        var endpoint = _container.GetConnectionString();
        _secretClient = KeyVaultHelper.GetSecretClient(endpoint);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _container.DisposeAsync();
    }

    [Test]
    public async Task CanStoreAndRetrieveSecret()
    {
        // Act
        await _secretClient.SetSecretAsync("test", "value");
        var secret = await _secretClient.GetSecretAsync("test");

        // Assert
        Assert.AreEqual("value", secret.Value.Value);
    }

    private string GetCertificatesPath()
    {
        // Return path to directory for certificates (will be auto-generated if missing)
        return Environment.GetEnvironmentVariable("KEYVAULT_CERTS_PATH") 
               ?? "/path/to/your/certificates";
    }
}
```

## MSTest Example

```csharp
using AzureKeyVaultEmulator.TestContainers;
using AzureKeyVaultEmulator.Aspire.Client;
using Azure.Security.KeyVault.Secrets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class KeyVaultTests
{
    private static AzureKeyVaultEmulatorContainer _container;
    private static SecretClient _secretClient;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        var certificatesPath = GetCertificatesPath();
        _container = new AzureKeyVaultEmulatorContainer(certificatesPath);
        await _container.StartAsync();

        var endpoint = _container.GetConnectionString();
        _secretClient = KeyVaultHelper.GetSecretClient(endpoint);
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await _container.DisposeAsync();
    }

    [TestMethod]
    public async Task CanStoreAndRetrieveSecret()
    {
        // Act
        await _secretClient.SetSecretAsync("test", "value");
        var secret = await _secretClient.GetSecretAsync("test");

        // Assert
        Assert.AreEqual("value", secret.Value.Value);
    }

    private static string GetCertificatesPath()
    {
        // Return path to directory for certificates (will be auto-generated if missing)
        return Environment.GetEnvironmentVariable("KEYVAULT_CERTS_PATH") 
               ?? "/path/to/your/certificates";
    }
}
```
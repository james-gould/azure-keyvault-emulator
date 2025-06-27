# TestContainers Module Usage Examples

This directory contains examples of how to use the Azure KeyVault Emulator TestContainers module in different scenarios.

## Basic Usage

```csharp
using AzureKeyVaultEmulator.TestContainers;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;

// Create container with certificate directory
var certificatesPath = "/path/to/certs"; // Must contain emulator.pfx
await using var container = new AzureKeyVaultEmulatorContainer(certificatesPath);

// Start the container
await container.StartAsync();

// Get the endpoint
var endpoint = container.GetConnectionString();

// Use with Azure SDK clients
var secretClient = new SecretClient(
    new Uri(endpoint), 
    new DefaultAzureCredential(),
    new SecretClientOptions 
    { 
        DisableChallengeResourceVerification = true 
    });

// Use the client
await secretClient.SetSecretAsync("test-secret", "test-value");
var secret = await secretClient.GetSecretAsync("test-secret");

// Container will be automatically disposed when using statement ends
```

## XUnit Test Example

```csharp
using AzureKeyVaultEmulator.TestContainers;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
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
        _secretClient = new SecretClient(
            new Uri(endpoint), 
            new DefaultAzureCredential(),
            new SecretClientOptions 
            { 
                DisableChallengeResourceVerification = true 
            });
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
        // Return path to directory containing emulator.pfx
        return Environment.GetEnvironmentVariable("KEYVAULT_CERTS_PATH") 
               ?? "/path/to/your/certificates";
    }
}
```

## NUnit Test Example

```csharp
using AzureKeyVaultEmulator.TestContainers;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
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
        _secretClient = new SecretClient(
            new Uri(endpoint), 
            new DefaultAzureCredential(),
            new SecretClientOptions 
            { 
                DisableChallengeResourceVerification = true 
            });
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
        // Return path to directory containing emulator.pfx
        return Environment.GetEnvironmentVariable("KEYVAULT_CERTS_PATH") 
               ?? "/path/to/your/certificates";
    }
}
```

## MSTest Example

```csharp
using AzureKeyVaultEmulator.TestContainers;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
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
        _secretClient = new SecretClient(
            new Uri(endpoint), 
            new DefaultAzureCredential(),
            new SecretClientOptions 
            { 
                DisableChallengeResourceVerification = true 
            });
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
        // Return path to directory containing emulator.pfx
        return Environment.GetEnvironmentVariable("KEYVAULT_CERTS_PATH") 
               ?? "/path/to/your/certificates";
    }
}
```

## Certificate Requirements

The TestContainers module requires a directory containing valid SSL certificates:

- `emulator.pfx` - Required PFX certificate file
- `emulator.crt` - Optional CRT certificate file

You can generate these certificates using the existing Azure KeyVault Emulator tools or by following the SSL certificate setup instructions in the main documentation.
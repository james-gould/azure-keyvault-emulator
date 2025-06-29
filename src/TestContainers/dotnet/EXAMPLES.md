# TestContainers Module Usage Examples

This directory contains examples of how to use the Azure KeyVault Emulator TestContainers module in different scenarios.

[You can read more about the inner working of the container here.](./README.md)

## CI/CD Usage

When using the TestContainers module in CI/CD environments like GitHub Actions, you can use the system's temporary directory for certificate storage. This ensures a clean environment for each run:

```csharp
using AzureKeyVaultEmulator.TestContainers;

// Use system temp directory for CI/CD environments
var certificatesPath = Path.GetTempPath();
await using var container = new AzureKeyVaultEmulatorContainer(certificatesPath);

// The container will automatically generate certificates in the temp directory
await container.StartAsync();

// Get an official AzureSDK SecretClient
var client = container.GetSecretClient();

// Use as normal
var secret = await client.SetSecretAsync("ApiKey", "12345");
```

On GitHub Actions, the temp directory is automatically cleaned between runs, making it ideal for ephemeral testing environments.

## Basic Usage

```csharp
using AzureKeyVaultEmulator.TestContainers;

// Create container with certificate directory and persistence
await using var container = new AzureKeyVaultEmulatorContainer();

// Start the container
await container.StartAsync();

// Get a AzureSDK KeyClient configured for the container
var keyClient = container.GetKeyClient();

// Get a AzureSDK SecretClient configured for the container
var secretClient = container.GetSecretClient();

// Get a AzureSDK CertificateClient configured for the container
var certificateClient = container.GetCertificateClient();

// Use as normal
var secret = await secretClient.SetSecretAsync("mySecretName", "mySecretValue");
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
        _container = new AzureKeyVaultEmulatorContainer();
        await _container.StartAsync();

        _secretClient = _container.GetSecretClient();
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
        _container = new AzureKeyVaultEmulatorContainer();
        await _container.StartAsync();

        _secretClient = _container.GetSecretClient();
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
        _container = new AzureKeyVaultEmulatorContainer();
        await _container.StartAsync();

        _secretClient = _container.GetSecretClient();
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
}
```
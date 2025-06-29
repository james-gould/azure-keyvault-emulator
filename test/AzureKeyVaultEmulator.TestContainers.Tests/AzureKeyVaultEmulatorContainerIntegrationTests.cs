using Xunit;
using AzureKeyVaultEmulator.TestContainers.Helpers;
using AzureKeyVaultEmulator.TestContainers.Constants;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure;

namespace AzureKeyVaultEmulator.TestContainers.Tests;

/// <summary>
/// Integration tests for the AzureKeyVaultEmulatorContainer.
/// These tests demonstrate real usage patterns but require Docker to be available.
/// </summary>
public class AzureKeyVaultEmulatorContainerIntegrationTests
{
    [Fact]
    public async Task ContainerCanStartAndStopSuccessfully()
    {
        await using var container = new AzureKeyVaultEmulatorContainer();

        await container.StartAsync();

        var endpoint = container.GetConnectionString();
        Assert.StartsWith("https://", endpoint);
        Assert.Contains("4997", endpoint);

        var port = container.GetMappedPublicPort(AzureKeyVaultEmulatorContainerConstants.Port);
        Assert.Equal(AzureKeyVaultEmulatorContainerConstants.Port, port);

        await container.StopAsync();
    }

    [Fact]
    public async Task ContainerCanPersistSecretsCorrectly()
    {
        await using var container = new AzureKeyVaultEmulatorContainer();

        await container.StartAsync();

        var client = container.GetSecretClient();

        var secretName = Guid.NewGuid().ToString();
        var secretValue = Guid.NewGuid().ToString();

        var createOperation = await client.SetSecretAsync(secretName, secretValue);

        Assert.Equal(secretValue, createOperation.Value.Value);

        var fromStore = await client.GetSecretAsync(secretName);

        Assert.Equal(secretValue, fromStore.Value.Value);

        await container.StopAsync();
    }

    [Fact]
    public async Task ContainerCanPersistCertificatesCorrectly()
    {
        await using var container = new AzureKeyVaultEmulatorContainer();

        await container.StartAsync();

        var client = container.GetCertificateClient();

        var certName = Guid.NewGuid().ToString();

        var createOperation = await client.StartCreateCertificateAsync(certName, CertificatePolicy.Default);

        await createOperation.WaitForCompletionAsync();

        var createdCert = createOperation.Value;

        Assert.Equal(certName, createdCert.Name);

        var fromStore = await client.GetCertificateAsync(certName);

        Assert.Equal(certName, fromStore.Value.Name);
    }

    [Fact]
    public async Task ContainerCanPersistKeysCorrectly()
    {
        await using var container = new AzureKeyVaultEmulatorContainer();

        await container.StartAsync();

        var client = container.GetKeyClient();

        var keyName = Guid.NewGuid().ToString();

        var createResponse = await client.CreateKeyAsync(keyName, KeyType.Rsa);

        Assert.Equal(keyName, createResponse.Value.Name);

        var fromStore = await client.GetKeyAsync(keyName);

        Assert.Equal(keyName, fromStore.Value.Name);
    }

    [Fact]
    public async Task CreatingSetupDatabaseWillPersistBetweenRuns()
    {
        var secretName = Guid.NewGuid().ToString();
        var secretValue = Guid.NewGuid().ToString();

        // Mark with -e Persist=true to create an SQLite Database
        await using var container = new AzureKeyVaultEmulatorContainer(persist: true);

        await container.StartAsync();

        var setupClient = container.GetSecretClient();

        await Assert.ThrowsAsync<RequestFailedException>(() => setupClient.GetSecretAsync(secretName));

        var setupSecret = await setupClient.SetSecretAsync(secretName, secretValue);

        Assert.Equal(secretName, setupSecret.Value.Name);
        Assert.Equal(secretValue, setupSecret.Value.Value);

        var fromStoreAfterSetup = await setupClient.GetSecretAsync(secretName);

        Assert.Equal(secretName, fromStoreAfterSetup.Value.Name);
        Assert.Equal(secretValue, fromStoreAfterSetup.Value.Value);

        // Kill the setup container
        await container.StopAsync();

        // Create another with -e Persist=true so it uses the existing SQLite Database.
        await using var secondaryContainer = new AzureKeyVaultEmulatorContainer(persist: true);

        await secondaryContainer.StartAsync();

        // Get secondary client, acting as if this is done within a test class using a fixture
        var secondaryClient = secondaryContainer.GetSecretClient();

        var secretFromSecondaryStore = await secondaryClient.GetSecretAsync(secretName);

        Assert.Equal(secretName, secretFromSecondaryStore.Value.Name);
        Assert.Equal(secretValue, secretFromSecondaryStore.Value.Value);
        Assert.Equal(fromStoreAfterSetup.Value.Value, secretFromSecondaryStore.Value.Value);
    }
}

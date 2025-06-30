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
public class AzureKeyVaultEmulatorContainerIntegrationTests : IAsyncLifetime
{
    private AzureKeyVaultEmulatorContainer? _container;

    public async Task InitializeAsync()
    {
        _container = new AzureKeyVaultEmulatorContainer();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if(_container is not null)
            await _container.DisposeAsync();
    }

    [Fact(Skip = "Requires Docker on GitHub Actions")]
    public void ContainerCanStartAndStopSuccessfully()
    {
        ArgumentNullException.ThrowIfNull(_container);

        var endpoint = _container.GetConnectionString();
        Assert.StartsWith("https://", endpoint);
        Assert.Contains("4997", endpoint);

        var port = _container.GetMappedPublicPort(AzureKeyVaultEmulatorContainerConstants.Port);
        Assert.Equal(AzureKeyVaultEmulatorContainerConstants.Port, port);
    }

    [Fact(Skip = "Requires Docker on GitHub Actions")]
    public async Task ContainerCanPersistSecretsCorrectly()
    {
        ArgumentNullException.ThrowIfNull(_container);

        var secretClient = _container.GetSecretClient();

        var secretName = Guid.NewGuid().ToString();
        var secretValue = Guid.NewGuid().ToString();

        var createOperation = await secretClient.SetSecretAsync(secretName, secretValue);

        Assert.Equal(secretValue, createOperation.Value.Value);

        var fromStore = await secretClient.GetSecretAsync(secretName);

        Assert.Equal(secretValue, fromStore.Value.Value);
    }

    [Fact(Skip = "Requires Docker on GitHub Actions")]
    public async Task ContainerCanPersistCertificatesCorrectly()
    {
        ArgumentNullException.ThrowIfNull(_container);

        var certClient = _container.GetCertificateClient();

        var certName = Guid.NewGuid().ToString();

        var createOperation = await certClient.StartCreateCertificateAsync(certName, CertificatePolicy.Default);

        await createOperation.WaitForCompletionAsync();

        var createdCert = createOperation.Value;

        Assert.Equal(certName, createdCert.Name);

        var fromStore = await certClient.GetCertificateAsync(certName);

        Assert.Equal(certName, fromStore.Value.Name);
    }

    [Fact(Skip = "Requires Docker on GitHub Actions")]
    public async Task ContainerCanPersistKeysCorrectly()
    {
        ArgumentNullException.ThrowIfNull(_container);

        var client = _container.GetKeyClient();

        var keyName = Guid.NewGuid().ToString();

        var createResponse = await client.CreateKeyAsync(keyName, KeyType.Rsa);

        Assert.Equal(keyName, createResponse.Value.Name);

        var fromStore = await client.GetKeyAsync(keyName);

        Assert.Equal(keyName, fromStore.Value.Name);
    }

    [Fact(Skip = "Dev run only while port is unconfigurable, error: Bind for 0.0.0.0:4997 failed: port is already allocated")]
    public async Task CreatingSetupDatabaseWillPersistBetweenRuns()
    {
        var secretName = Guid.NewGuid().ToString();
        var secretValue = Guid.NewGuid().ToString();

        // Marks with -e Persist=true to create an SQLite Database
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

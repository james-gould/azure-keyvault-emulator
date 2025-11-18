using Xunit;
using AzureKeyVaultEmulator.TestContainers.Helpers;
using AzureKeyVaultEmulator.TestContainers.Constants;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure;
using System.Runtime.InteropServices;

namespace AzureKeyVaultEmulator.TestContainers.Tests;

/// <summary>
/// Integration tests for the AzureKeyVaultEmulatorContainer.
/// These tests demonstrate real usage patterns but require Docker to be available.
/// </summary>
public class AzureKeyVaultEmulatorContainerIntegrationTests
{
    private static async Task<AzureKeyVaultEmulatorContainer> CreateContainerAsync(bool assignRandomHostPort)
    {
        var tag = "latest";

        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            tag += "-arm";

        var options = new AzureKeyVaultEmulatorOptions
        {
            AssignRandomHostPort = assignRandomHostPort,
            LocalCertificatePath = Path.GetTempPath(),
            Tag = tag
        };
        var container = new AzureKeyVaultEmulatorContainer(options);

        await container.StartAsync();

        return container;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ContainerCanStartAndStopSuccessfully(bool assignRandomHostPort)
    {
        await using var container = await CreateContainerAsync(assignRandomHostPort);

        var endpoint = container.GetConnectionString();
        Assert.StartsWith("https://", endpoint);

        if(!assignRandomHostPort)
        {
            Assert.Contains("4997", endpoint);

            var port = container.GetMappedPublicPort(AzureKeyVaultEmulatorContainerConstants.Port);
            Assert.Equal(AzureKeyVaultEmulatorContainerConstants.Port, port);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ContainerCanPersistSecretsCorrectly(bool assignRandomHostPort)
    {
        await using var container = await CreateContainerAsync(assignRandomHostPort);
        var secretClient = container.GetSecretClient();

        var secretName = Guid.NewGuid().ToString();
        var secretValue = Guid.NewGuid().ToString();

        var createOperation = await secretClient.SetSecretAsync(secretName, secretValue);

        Assert.Equal(secretValue, createOperation.Value.Value);

        var fromStore = await secretClient.GetSecretAsync(secretName);

        Assert.Equal(secretValue, fromStore.Value.Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ContainerCanPersistCertificatesCorrectly(bool assignRandomHostPort)
    {
        await using var container = await CreateContainerAsync(assignRandomHostPort);
        var certClient = container.GetCertificateClient();

        var certName = Guid.NewGuid().ToString();

        var createOperation = await certClient.StartCreateCertificateAsync(certName, CertificatePolicy.Default);

        await createOperation.WaitForCompletionAsync();

        var createdCert = createOperation.Value;

        Assert.Equal(certName, createdCert.Name);

        var fromStore = await certClient.GetCertificateAsync(certName);

        Assert.Equal(certName, fromStore.Value.Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ContainerCanPersistKeysCorrectly(bool assignRandomHostPort)
    {
        await using var container = await CreateContainerAsync(assignRandomHostPort);
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

        // Marks with -e Persist=true to create an SQLite Database
        await using var container = new AzureKeyVaultEmulatorContainer(persist: true, tag: "latest", assignRandomHostPort: true);

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
        await using var secondaryContainer = new AzureKeyVaultEmulatorContainer(persist: true, tag: "latest");

        await secondaryContainer.StartAsync();

        // Get secondary client, acting as if this is done within a test class using a fixture
        var secondaryClient = secondaryContainer.GetSecretClient();

        var secretFromSecondaryStore = await secondaryClient.GetSecretAsync(secretName);

        Assert.Equal(secretName, secretFromSecondaryStore.Value.Name);
        Assert.Equal(secretValue, secretFromSecondaryStore.Value.Value);
        Assert.Equal(fromStoreAfterSetup.Value.Value, secretFromSecondaryStore.Value.Value);
    }
}

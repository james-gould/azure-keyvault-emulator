using Xunit;
using AzureKeyVaultEmulator.TestContainers.Helpers;
using AzureKeyVaultEmulator.TestContainers.Constants;

namespace AzureKeyVaultEmulator.TestContainers.Tests;

/// <summary>
/// Integration tests for the AzureKeyVaultEmulatorContainer.
/// These tests demonstrate real usage patterns but require Docker to be available.
/// </summary>
public class AzureKeyVaultEmulatorContainerIntegrationTests
{
    //[Fact(Skip = "Integration test - requires Docker")]
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

    //[Fact(Skip = "Integration test - requires Docker")]
    [Fact]
    public async Task ContainerWithPersistenceConfiguresCorrectly()
    {
        await using var container = new AzureKeyVaultEmulatorContainer();

        await container.StartAsync();

        var endpoint = container.GetConnectionString();
        Assert.StartsWith("https://", endpoint);

        var client = container.GetSecretClient();

        var secretName = Guid.NewGuid().ToString();
        var secretValue = Guid.NewGuid().ToString();

        var createOperation = await client.SetSecretAsync(secretName, secretValue);

        Assert.Equal(secretValue, createOperation.Value.Value);

        var fromStore = await client.GetSecretAsync(secretName);

        Assert.Equal(secretValue, fromStore.Value.Value);

        await container.StopAsync();
    }
}

using Aspire.Hosting;
using AzureKeyVaultEmulator.Aspire.Hosting;

namespace AzureKeyVaultEmulator.IntegrationTests.Emulator;

/// <summary>
/// <para>Verifies that <see cref="KeyVaultEmulatorOptions.Port"/> controls the host port the emulator
/// container is exposed on. A fixed value pins the port so the vault URI (<c>https://localhost:{port}</c>)
/// remains stable between runs, while <see langword="null"/> preserves the existing random host port behaviour.</para>
/// <para>Also verifies that enabling <see cref="KeyVaultEmulatorOptions.Persist"/> without a static
/// <see cref="KeyVaultEmulatorOptions.Port"/> fails fast, since persisted data embeds the port in its identifiers.</para>
/// </summary>
public sealed class EmulatorPortConfigurationTests
{
    private const string _resourceName = "keyvault-emulator";
    private const string _httpsEndpointName = "https";

    [Fact]
    public void StaticPortIsAppliedToEmulatorHttpsEndpoint()
    {
        const int staticPort = 45123;

        var builder = DistributedApplication.CreateBuilder();

        var keyVault = builder
            .AddAzureKeyVault(_resourceName)
            .RunAsEmulator(new KeyVaultEmulatorOptions { Port = staticPort });

        var endpoint = GetHttpsEndpoint(keyVault);

        Assert.Equal(staticPort, endpoint.Port);
    }

    [Fact]
    public void NullPortLeavesEmulatorHttpsEndpointDynamic()
    {
        var builder = DistributedApplication.CreateBuilder();

        var keyVault = builder
            .AddAzureKeyVault(_resourceName)
            .RunAsEmulator(new KeyVaultEmulatorOptions());

        var endpoint = GetHttpsEndpoint(keyVault);

        Assert.Null(endpoint.Port);
    }

    [Fact]
    public void StaticPortIsAppliedWhenAddingEmulatorDirectly()
    {
        const int staticPort = 46123;

        var builder = DistributedApplication.CreateBuilder();

        var keyVault = builder
            .AddAzureKeyVaultEmulator(_resourceName, new KeyVaultEmulatorOptions { Port = staticPort });

        var endpoint = GetHttpsEndpoint(keyVault);

        Assert.Equal(staticPort, endpoint.Port);
    }

    [Fact]
    public void EnablingPersistWithoutPortThrows()
    {
        var builder = DistributedApplication.CreateBuilder();

        var exception = Assert.ThrowsAny<Exception>(() => builder
            .AddAzureKeyVault(_resourceName)
            .RunAsEmulator(new KeyVaultEmulatorOptions { Persist = true }));

        AssertPersistRequiresPort(exception);
    }

    [Fact]
    public void EnablingPersistWithoutPortThrowsWhenAddingEmulatorDirectly()
    {
        var builder = DistributedApplication.CreateBuilder();

        var exception = Assert.ThrowsAny<Exception>(() => builder
            .AddAzureKeyVaultEmulator(_resourceName, new KeyVaultEmulatorOptions { Persist = true }));

        AssertPersistRequiresPort(exception);
    }

    [Fact]
    public void EnablingPersistWithStaticPortIsAllowed()
    {
        const int staticPort = 47123;

        var builder = DistributedApplication.CreateBuilder();

        var keyVault = builder
            .AddAzureKeyVault(_resourceName)
            .RunAsEmulator(new KeyVaultEmulatorOptions { Persist = true, Port = staticPort });

        var endpoint = GetHttpsEndpoint(keyVault);

        Assert.Equal(staticPort, endpoint.Port);
    }

    private static void AssertPersistRequiresPort(Exception exception)
    {
        // KeyVaultEmulatorException is internal, so assert on the type name and the message contents.
        Assert.Equal("KeyVaultEmulatorException", exception.GetType().Name);
        Assert.Contains(nameof(KeyVaultEmulatorOptions.Persist), exception.Message);
        Assert.Contains(nameof(KeyVaultEmulatorOptions.Port), exception.Message);
    }

    private static EndpointAnnotation GetHttpsEndpoint<T>(IResourceBuilder<T> resource)
        where T : IResource
    {
        var endpoint = resource.Resource.Annotations
            .OfType<EndpointAnnotation>()
            .SingleOrDefault(e => e.Name == _httpsEndpointName);

        Assert.NotNull(endpoint);

        return endpoint!;
    }
}

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace AzureKeyVaultEmulator.TestContainers;

/// <summary>
/// Represents a TestContainer for the Azure KeyVault Emulator.
/// </summary>
public sealed class AzureKeyVaultEmulatorContainer : IAsyncDisposable, IDisposable
{
    private readonly IContainer _container;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultEmulatorContainer"/> class.
    /// </summary>
    /// <param name="certificatesDirectory">The host directory containing SSL certificates.</param>
    /// <param name="persist">Whether to enable data persistence.</param>
    public AzureKeyVaultEmulatorContainer(string certificatesDirectory, bool persist = true)
    {
        TryValidateCertificatesDirectory(certificatesDirectory);

        _container = new ContainerBuilder()
            .WithImage($"{AzureKeyVaultEmulatorConstants.Registry}/{AzureKeyVaultEmulatorConstants.Image}:{AzureKeyVaultEmulatorConstants.Tag}")
            .WithPortBinding(AzureKeyVaultEmulatorConstants.Port, true)
            .WithBindMount(certificatesDirectory, AzureKeyVaultEmulatorConstants.CertificatesMountPath)
            .WithEnvironment(AzureKeyVaultEmulatorConstants.PersistEnvironmentVariable, $"{persist}")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(AzureKeyVaultEmulatorConstants.Port))
            .Build();
    }

    /// <summary>
    /// Gets the container ID.
    /// </summary>
    public string Id => _container.Id;

    /// <summary>
    /// Gets the container name.
    /// </summary>
    public string Name => _container.Name;

    /// <summary>
    /// Gets the container IP address.
    /// </summary>
    public string IpAddress => _container.IpAddress;

    /// <summary>
    /// Gets the container hostname.
    /// </summary>
    public string Hostname => _container.Hostname;

    /// <summary>
    /// Gets the connection string for the Azure KeyVault Emulator.
    /// </summary>
    /// <returns>The HTTPS endpoint URL for the emulator.</returns>
    public string GetConnectionString() => GetEndpoint();

    /// <summary>
    /// Gets the endpoint URL for the Azure KeyVault Emulator.
    /// </summary>
    /// <returns>The HTTPS endpoint URL for the emulator.</returns>
    public string GetEndpoint()
    {
        var port = GetMappedPublicPort(AzureKeyVaultEmulatorConstants.Port);
        return $"https://{Hostname}:{port}";
    }

    /// <summary>
    /// Gets the mapped public port for the specified container port.
    /// </summary>
    /// <param name="containerPort">The container port.</param>
    /// <returns>The mapped public port.</returns>
    public ushort GetMappedPublicPort(int containerPort = AzureKeyVaultEmulatorConstants.Port) => _container.GetMappedPublicPort(containerPort);

    /// <summary>
    /// Starts the container.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task StartAsync(CancellationToken ct = default) => _container.StartAsync(ct);

    /// <summary>
    /// Stops the container.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken ct = default) => _container.StopAsync(ct);

    /// <summary>
    /// Disposes the container.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        return _container.DisposeAsync();
    }

    /// <summary>
    /// Disposes the container synchronously.
    /// </summary>
    public void Dispose()
    {
        _container.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Validates the certificates directory and required files.
    /// </summary>
    /// <param name="certificatesDirectory">The certificates directory path.</param>
    /// <exception cref="ArgumentException">Thrown when the directory path is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the required emulator.pfx file is not found in the directory.</exception>
    private static void TryValidateCertificatesDirectory(string certificatesDirectory)
    {
        if (string.IsNullOrWhiteSpace(certificatesDirectory))
        {
            throw new ArgumentException("Certificates directory path cannot be null or empty.", nameof(certificatesDirectory));
        }

        if (!Directory.Exists(certificatesDirectory))
        {
            throw new DirectoryNotFoundException($"Certificates directory not found: {certificatesDirectory}");
        }

        var pfxPath = Path.Combine(certificatesDirectory, AzureKeyVaultEmulatorConstants.RequiredPfxFileName);

        if (!File.Exists(pfxPath))
        {
            throw new FileNotFoundException(
                $"Required certificate file '{AzureKeyVaultEmulatorConstants.RequiredPfxFileName}' not found in directory: {certificatesDirectory}. " +
                "If running locally run \"bash docs/setup.sh\" to generate the required SSL certificates.");
        }
    }
}
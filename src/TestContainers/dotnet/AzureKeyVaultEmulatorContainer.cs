using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using AzureKeyVaultEmulator.TestContainers.Constants;
using AzureKeyVaultEmulator.TestContainers.Models;
using AzureKeyVaultEmulator.TestContainers.Helpers;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;

namespace AzureKeyVaultEmulator.TestContainers;

/// <summary>
/// Represents a TestContainer for the Azure KeyVault Emulator.
/// </summary>
public sealed class AzureKeyVaultEmulatorContainer : IAsyncDisposable, IDisposable
{
    private readonly IContainer _container;
    private static CertificateLoaderVM? _loadedCertificates;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultEmulatorContainer"/> class.
    /// </summary>
    /// <param name="certificatesDirectory">The optional host directory containing SSL certificates. If not provided the certificate will be generated to your User profile.</param>
    /// <param name="persist">Whether to enable data persistence.</param>
    /// <param name="generateCertificates">Whether to automatically generate SSL certificates if they don't exist.</param>
    public AzureKeyVaultEmulatorContainer(
        string? certificatesDirectory = null,
        bool persist = true,
        bool generateCertificates = true)
    {
        var options = new KeyVaultEmulatorOptions
        {
            Persist = persist,
            ShouldGenerateCertificates = generateCertificates,
            LocalCertificatePath = certificatesDirectory ?? string.Empty,
        };

        _loadedCertificates = AzureKeyVaultEmulatorCertHelper.ValidateOrGenerateCertificate(options);

        if (options.LoadCertificatesIntoTrustStore)
            AzureKeyVaultEmulatorCertHelper.TryWriteToStore(options, _loadedCertificates.Pfx, _loadedCertificates.LocalCertificatePath, _loadedCertificates.pem);

        _container = new ContainerBuilder()
            .WithImage($"{AzureKeyVaultEmulatorContainerConstants.Registry}/{AzureKeyVaultEmulatorContainerConstants.Image}:{AzureKeyVaultEmulatorContainerConstants.Tag}")
            .WithPortBinding(AzureKeyVaultEmulatorContainerConstants.Port, false)
            .WithBindMount(certificatesDirectory, AzureKeyVaultEmulatorCertConstants.CertMountTarget)
            .WithEnvironment(AzureKeyVaultEmulatorContainerConstants.PersistData, $"{persist}")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(AzureKeyVaultEmulatorContainerConstants.Port))
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
        var port = GetMappedPublicPort(AzureKeyVaultEmulatorContainerConstants.Port);
        return $"https://{Hostname}:{port}";
    }

    /// <summary>
    /// Gets the mapped public port for the specified container port.
    /// </summary>
    /// <param name="containerPort">The container port.</param>
    /// <returns>The mapped public port.</returns>
    public ushort GetMappedPublicPort(int containerPort = AzureKeyVaultEmulatorContainerConstants.Port)
        => _container.GetMappedPublicPort(containerPort);

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

    private static void UninstallContainerCertificates()
    {
        var thumbprint = _loadedCertificates?.Pfx?.Thumbprint;

        if (string.IsNullOrEmpty(thumbprint))
            return; // hmm

        using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);

        var certsToRemove = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);

        foreach (var cert in certsToRemove)
            store.Remove(cert);

        store.Close();
    }

    /// <summary>
    /// Disposes the container.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        if (OperatingSystem.IsWindows())
        {
            UninstallContainerCertificates();
        }
        else
        {
            Debug.WriteLine($"To remove the container certificates you must remove {AzureKeyVaultEmulatorCertConstants.Crt} from your Trusted Root CA store in the User location.");
            Debug.WriteLine(@"Execute sudo rm /usr/local/share/ca-certificates/mycert.crt \n sudo update-ca-certificates --fresh");
        }

            return _container.DisposeAsync();
    }

    /// <summary>
    /// Disposes the container synchronously.
    /// </summary>
    public void Dispose()
    {
        _container.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

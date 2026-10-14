using Aspire.Hosting.Azure;

namespace AzureKeyVaultEmulator.Aspire.Hosting;

public static partial class KeyVaultEmulatorExtensions
{
    /// <inheritdoc cref="AddAzureKeyVaultEmulator"/>
    [AspireExport("addAzureKeyVaultEmulator")]
    internal static IResourceBuilder<AzureKeyVaultResource> AddAzureKeyVaultEmulatorForExport(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        KeyVaultEmulatorExportOptions? options = null)
        => builder.AddAzureKeyVaultEmulator(name, options?.ToOptions());

    /// <inheritdoc cref="RunAsEmulator"/>
    [AspireExport("runAsEmulator")]
    internal static IResourceBuilder<AzureKeyVaultResource> RunAsEmulatorForExport(
        this IResourceBuilder<AzureKeyVaultResource> builder,
        KeyVaultEmulatorExportOptions? options = null,
        string? configSectionName = null)
        => builder.RunAsEmulator(options?.ToOptions(), configSectionName);
}

[AspireDto]
internal sealed class KeyVaultEmulatorExportOptions
{
    /// <inheritdoc cref="KeyVaultEmulatorOptions.Lifetime"/>
    public ContainerLifetime? Lifetime { get; init; }
    /// <inheritdoc cref="KeyVaultEmulatorOptions.Port"/>
    public int? Port { get; init; }
    /// <inheritdoc cref="KeyVaultEmulatorOptions.Persist"/>
    public bool? Persist { get; init; }
    /// <inheritdoc cref="KeyVaultEmulatorOptions.LocalCertificatePath"/>
    public string? LocalCertificatePath { get; init; }
    /// <inheritdoc cref="KeyVaultEmulatorOptions.LoadCertificatesIntoTrustStore"/>
    public bool? LoadCertificatesIntoTrustStore { get; init; }
    /// <inheritdoc cref="KeyVaultEmulatorOptions.ShouldGenerateCertificates"/>
    public bool? ShouldGenerateCertificates { get; init; }
    /// <inheritdoc cref="KeyVaultEmulatorOptions.UseDotnetDevCerts"/>
    public bool? UseDotnetDevCerts { get; init; }

    internal KeyVaultEmulatorOptions ToOptions()
    {
        var options = new KeyVaultEmulatorOptions();
        options.Lifetime = Lifetime ?? options.Lifetime;
        options.Port = Port ?? options.Port;
        options.Persist = Persist ?? options.Persist;
        options.LocalCertificatePath = LocalCertificatePath ?? options.LocalCertificatePath;
        options.LoadCertificatesIntoTrustStore = LoadCertificatesIntoTrustStore ?? options.LoadCertificatesIntoTrustStore;
        options.ShouldGenerateCertificates = ShouldGenerateCertificates ?? options.ShouldGenerateCertificates;
        options.UseDotnetDevCerts = UseDotnetDevCerts ?? options.UseDotnetDevCerts;
        return options;
    }
}

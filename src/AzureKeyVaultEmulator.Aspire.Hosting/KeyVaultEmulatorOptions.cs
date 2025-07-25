using System.Security.Cryptography.X509Certificates;

namespace AzureKeyVaultEmulator.Aspire.Hosting;

/// <summary>
/// Allows for granular configuration of the Azure Key Vault Emulator.
/// </summary>
public sealed class KeyVaultEmulatorOptions
{
    /// <summary>
    /// Sets the lifetime of the Azure Key Vault Emulator container on the host machine.
    /// </summary>
    public ContainerLifetime Lifetime { get; set; } = ContainerLifetime.Session;

    /// <summary>
    /// Allows the Emulator to persist data beyond temporary storage for multi-session use.
    /// </summary>
    public bool Persist { get; set; } = false;

    /// <summary>
    /// <para>Specify the directory to be used as a mount for the Azure Key Vault Emulator.</para>
    /// <para>Warning: your container runtime must have read access to this directory.</para>
    /// </summary>
    public string LocalCertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// <para>Determines if the Emulator should attempt to load the certificates into the host machine's trust store.</para>
    /// <para>Unused if the certificates are already present, removing the administration privilege requirement.</para>
    /// </summary>
    public bool LoadCertificatesIntoTrustStore { get; set; } = true;

    /// <summary>
    /// <para>Disables the Azure Key Vault Emulator creating a self signed SSL certificate for you at runtime.</para>
    /// <para>
    /// Using this option will require you to provide a certificate in PFX (and optionally a CRT) format within the same directory.
    /// The directory must also be set via <see cref="LocalCertificatePath"/>.
    /// </para>
    /// <para>The PFX password MUST be "emulator" - all lowercase without the double quotes. This limitation is being looked into.</para>
    /// </summary>
    public bool ShouldGenerateCertificates { get; set; } = true;

    /// <summary>
    /// <para>Instructs the AzureKeyVaultEmulator.Aspire.Hosting runtime to create and install certificates using the dotnet dev-certs command instead of OpenSSL.</para>
    /// <para>If you experience issues with mocking tools that enforce SSL, this option may resolve them.</para>
    /// <see href="https://github.com/james-gould/azure-keyvault-emulator/issues/293"/>
    /// </summary>
    public bool UseDotnetDevCerts { get; set; } = false;

    /// <summary>
    /// <para>Cleans up the generated SSL certificates on application shutdown.</para>
    /// <para>If you do not set a value for <see cref="LocalCertificatePath"/>, the default local user directory will be used for your OS.</para>
    /// <para>Default: <see langword="false"/></para>
    /// </summary>
    public bool ForceCleanupOnShutdown { get; set; } = false;

    /// <summary>
    /// Used to internally validate the configuration of the emulator before performing any IO.
    /// </summary>
    internal bool IsValidCustomisable
        => ShouldGenerateCertificates
            // Validates that the Emulator can generate a self signed SSL certificate and load into the trust store.
            ? ShouldGenerateCertificates && LoadCertificatesIntoTrustStore
            // Validates the host machine has provided a local path containing preconfigured certificates
            : !string.IsNullOrEmpty(LocalCertificatePath);

    /// <summary>
    /// Used to carry the PFX through the generation and installation lifetime. Not passed as an option.
    /// </summary>
    internal X509Certificate2? PFX { get; set; }

    /// <summary>
    /// Used to carry the CRT through the generation and installation lifetime. Not passed as an option.
    /// </summary>
    internal string? CRT { get; set; }
}

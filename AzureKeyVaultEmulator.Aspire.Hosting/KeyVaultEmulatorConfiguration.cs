namespace AzureKeyVaultEmulator.Aspire.Hosting;

/// <summary>
/// Allows for granular configuration of the Azure Key Vault Emulator.
/// </summary>
public sealed class KeyVaultEmulatorConfiguration
{
    /// <summary>
    /// <para>Specify the directory to be used as a mount for the Azure Key Vault Emulator.</para>
    /// <para>Warning: your container runtime must have read access to this directory.</para>
    /// </summary>
    public string LocalCertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// <para>Disables the Azure Key Vault Emulator creating a self signed SSL certificate for you at runtime.</para>
    /// <para>
    /// Using this option will require you to provide a certificate in PFX and CRT format within the same directory.
    /// The directory must be set via <see cref="LocalCertificatePath"/> and contain both files.
    /// </para>
    /// <para>The PFX password MUST be "emulator" - all lowercase without the double quotes. This limitation is being looked into.</para>
    /// </summary>
    public bool ShouldGenerateCertificates { get; set; } = true;

    /// <summary>
    /// <para>Hooks in an IHostLifetime service to remove the certificates from your local machine on AppHost shutdown.</para>
    /// <para>If you do not set a value for <see cref="LocalCertificatePath"/> the default local user directory will be used for your OS.</para>
    /// <para>Default: <see langword="false"/></para>
    /// </summary>
    public bool ForceCleanupOnShutdown { get; set; } = false;

    /// <summary>
    /// Used to internally validate the configuration of the emulator before performing any IO.
    /// </summary>
    internal bool IsValidCustomisable
        => ShouldGenerateCertificates ? ShouldGenerateCertificates : !string.IsNullOrEmpty(LocalCertificatePath);
}

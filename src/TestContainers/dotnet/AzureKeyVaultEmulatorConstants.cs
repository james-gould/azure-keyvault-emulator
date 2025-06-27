namespace AzureKeyVaultEmulator.TestContainers;

/// <summary>
/// Constants for the Azure KeyVault Emulator container configuration.
/// </summary>
public static class AzureKeyVaultEmulatorConstants
{
    /// <summary>
    /// The default Docker registry for the emulator image.
    /// </summary>
    public const string Registry = "docker.io";

    /// <summary>
    /// The emulator Docker image name.
    /// </summary>
    public const string Image = "jamesgoulddev/azure-keyvault-emulator";

    /// <summary>
    /// The default image tag.
    /// </summary>
    public const string Tag = "latest";

    /// <summary>
    /// The default port the emulator listens on.
    /// </summary>
    public const int Port = 4997;

    /// <summary>
    /// The container mount path for certificates.
    /// </summary>
    public const string CertificatesMountPath = "/certs";

    /// <summary>
    /// The required PFX certificate file name.
    /// </summary>
    public const string RequiredPfxFileName = "emulator.pfx";

    /// <summary>
    /// The environment variable name for persistence configuration.
    /// </summary>
    public const string PersistEnvironmentVariable = "Persist";
}
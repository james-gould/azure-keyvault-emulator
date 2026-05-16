namespace AzureKeyVaultEmulator.Aspire.Hosting;

internal partial class KeyVaultEmulatorContainerConstants
{
    // Image

    public const string Registry = "docker.io";
    public const string Image = "jamesgoulddev/azure-keyvault-emulator";
    public const int Port = 4997;

    public const string Tag = "3.0.1";
    public static string ArmTag => $"{Tag}-arm";

}

internal partial class KeyVaultEmulatorContainerConstants
{
    // Environment Variables

    public const string PersistData = "Persist";

    /// <summary>
    /// Name of the environment variable read by the emulator on startup to determine the tenant id
    /// it advertises in the <c>WWW-Authenticate</c> challenge header. Mirrors the Azure SDK's
    /// well-known <c>AZURE_TENANT_ID</c> env var so an Aspire host can simply propagate its value.
    /// </summary>
    public const string AzureTenantId = "AZURE_TENANT_ID";
}

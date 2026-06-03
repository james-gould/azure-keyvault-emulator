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
    /// it advertises in the <c>WWW-Authenticate</c> challenge header.
    /// </summary>
    public const string AzureTenantId = "AZURE_TENANT_ID";

    /// <summary>
    /// Placeholder tenant id used by the emulator's local OAuth2 surface. Mirrors
    /// AzureKeyVaultEmulator.Shared.Constants.AuthConstants.EmulatorTenantId; the hosting package
    /// targets earlier TFMs than the shared project, so the value is kept here too.
    /// </summary>
    public const string EmulatorTenantId = "a0c2a3f5-e1b3-4d6a-9c41-2cdd1f2c7e0f";
}

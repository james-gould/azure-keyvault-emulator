namespace AzureKeyVaultEmulator.Aspire.Hosting;

internal partial class KeyVaultEmulatorContainerConstants
{
    // Image

    public const string Registry = "docker.io";
    public const string Image = "jamesgoulddev/azure-keyvault-emulator";
    public const int Port = 4997;

    public const string Tag = "2.6.2";
    public static string ArmTag => $"{Tag}-arm";

}

internal partial class KeyVaultEmulatorContainerConstants
{
    // Environment Variables

    public const string PersistData = "Persist";
}

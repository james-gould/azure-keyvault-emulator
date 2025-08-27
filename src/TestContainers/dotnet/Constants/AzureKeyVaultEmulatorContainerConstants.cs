namespace AzureKeyVaultEmulator.TestContainers.Constants;

internal partial class AzureKeyVaultEmulatorContainerConstants
{
    // Image

    public const string Registry = "docker.io";
    public const string Image = "jamesgoulddev/azure-keyvault-emulator";
    public const int Port = 4997;

    public const string Tag = "2.5.9";
    public static string ArmTag => $"{Tag}-arm";
}

internal partial class AzureKeyVaultEmulatorContainerConstants
{
    // Environment Variables

    public const string PersistData = "Persist";
}

namespace AzureKeyVaultEmulator.TestContainers.Constants;

internal partial class AzureKeyVaultEmulatorContainerConstants
{
    // Image

    public const string Registry = "docker.io";
    public const string Image = "jamesgoulddev/azure-keyvault-emulator";
    public const int Port = 4997;

    public const string Tag = "latest";
}

internal partial class AzureKeyVaultEmulatorContainerConstants
{
    // Connection related

    public const string Endpoint = "https://localhost:4997";
}

internal partial class AzureKeyVaultEmulatorContainerConstants
{
    // Environment Variables

    public const string PersistData = "Persist";
}

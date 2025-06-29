namespace AzureKeyVaultEmulator.TestContainers.Constants;

public partial class AzureKeyVaultEmulatorContainerConstants
{
    // Image

    public const string Registry = "docker.io";
    public const string Image = "jamesgoulddev/azure-keyvault-emulator";
    public const int Port = 4997;

    public const string Tag = "latest";
}

public partial class AzureKeyVaultEmulatorContainerConstants
{
    // Connection related

    public const string Endpoint = "https://localhost:4997";
}

public partial class AzureKeyVaultEmulatorContainerConstants
{
    // Environment Variables

    public const string PersistData = "Persist";
}

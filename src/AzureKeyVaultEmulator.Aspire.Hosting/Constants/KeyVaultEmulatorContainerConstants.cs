namespace AzureKeyVaultEmulator.Aspire.Hosting
{
    internal partial class KeyVaultEmulatorContainerConstants
    {
        // Image

        public const string Registry = "docker.io";
        public const string Image = "jamesgoulddev/azure-keyvault-emulator";
        public const int Port = 4997;

        public const string Tag = "latest";
    }

    internal partial class KeyVaultEmulatorContainerConstants
    {
        // Connection related

        public const string Endpoint = "https://localhost:4997";
    }

    internal partial class KeyVaultEmulatorContainerConstants
    {
        // Environment Variables

        public const string PersistData = "Persist";
    }
}

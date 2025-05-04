namespace AzureKeyVaultEmulator.Aspire.Hosting
{
    internal partial class KeyVaultEmulatorConstants
    {
        // Container

        public const string Registry = "docker.io";
        public const string Image = "jamesgoulddev/azure-keyvault-emulator";
        public const string Tag = "latest";
        public const int Port = 4997;
    }

    internal partial class KeyVaultEmulatorConstants
    {
        // Connection related

        public const string Endpoint = "https://localhost:4997";
    }
}

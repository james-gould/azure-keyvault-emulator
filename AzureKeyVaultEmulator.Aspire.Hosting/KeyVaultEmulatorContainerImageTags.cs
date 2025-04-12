namespace AzureKeyVaultEmulator.Hosting.Aspire
{
    internal static class KeyVaultEmulatorContainerImageTags
    {
        public const string Registry = "docker.io"; // may not be needed? 

        public const string Image = "jamesgoulddev/azure-keyvault-emulator";
        public const string Tag = "latest";

        public const string Name = "keyvault-emulator";
        public const int Port = 4997;
    }
}

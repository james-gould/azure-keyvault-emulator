namespace AzureKeyVaultEmulator.Aspire.Hosting
{
    /// <summary>
    /// Wires a consumer up to the Azure Key Vault Emulator so that <c>DefaultAzureCredential</c>
    /// can authenticate against it without any real Azure credentials.
    /// </summary>
    public static class KeyVaultEmulatorCredentialExtensions
    {
        // GUID-shaped placeholders so MSAL's tenant-id / client-id validation is satisfied.
        private const string _emulatorClientId = "a0c2a3f5-e1b3-4d6a-9c41-2cdd1f2c7e0f";
        private const string _emulatorClientSecret = "emulator-client-secret";

        /// <summary>
        /// Sets the standard Azure SDK environment variables (<c>AZURE_TENANT_ID</c>,
        /// <c>AZURE_CLIENT_ID</c>, <c>AZURE_CLIENT_SECRET</c>, <c>AZURE_AUTHORITY_HOST</c>) on
        /// <paramref name="consumer"/> pointing at the emulator's local OAuth2 surface.
        /// </summary>
        /// <typeparam name="TConsumer">The resource that will consume the emulator.</typeparam>
        /// <typeparam name="TEmulator">The emulator resource exposing an https endpoint.</typeparam>
        /// <param name="consumer">The Aspire resource builder for the consumer.</param>
        /// <param name="emulator">The Aspire resource builder for the emulator itself.</param>
        /// <returns>The original <paramref name="consumer"/> for chaining.</returns>
        public static IResourceBuilder<TConsumer> WithAzureKeyVaultEmulatorCredentials<TConsumer, TEmulator>(
            this IResourceBuilder<TConsumer> consumer,
            IResourceBuilder<TEmulator> emulator)
            where TConsumer : IResourceWithEnvironment
            where TEmulator : IResourceWithEndpoints
        {
            ArgumentNullException.ThrowIfNull(consumer);
            ArgumentNullException.ThrowIfNull(emulator);

            return consumer
                .WithEnvironment("AZURE_AUTHORITY_HOST", emulator.GetEndpoint("https"))
                .WithEnvironment(ctx =>
                {
                    ctx.EnvironmentVariables["AZURE_TENANT_ID"] = KeyVaultEmulatorContainerConstants.EmulatorTenantId;
                    ctx.EnvironmentVariables["AZURE_CLIENT_ID"] = _emulatorClientId;
                    ctx.EnvironmentVariables["AZURE_CLIENT_SECRET"] = _emulatorClientSecret;
                });
        }
    }
}

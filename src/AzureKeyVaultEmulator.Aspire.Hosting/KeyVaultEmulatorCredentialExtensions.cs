namespace AzureKeyVaultEmulator.Aspire.Hosting
{
    /// <summary>
    /// Extension methods to wire a consumer (e.g. an <see cref="IResourceWithEnvironment"/> service or
    /// container) up to the Azure Key Vault Emulator in such a way that the official Azure SDK's
    /// <c>DefaultAzureCredential</c> can authenticate against the emulator without any real Azure
    /// credentials, and without requiring the consumer to take a dependency on emulator-specific client
    /// packages.
    /// </summary>
    public static class KeyVaultEmulatorCredentialExtensions
    {
        // Placeholder values used by the emulator when acting as its own OAuth2 / OIDC authority.
        // They are GUID-shaped so they pass MSAL's tenant-id / client-id validation.
        private const string _emulatorTenantId = "a0c2a3f5-e1b3-4d6a-9c41-2cdd1f2c7e0f";
        private const string _emulatorClientId = "a0c2a3f5-e1b3-4d6a-9c41-2cdd1f2c7e0f";
        private const string _emulatorClientSecret = "emulator-client-secret";

        /// <summary>
        /// Configures <paramref name="consumer"/> so that an in-process <c>DefaultAzureCredential</c>
        /// (specifically, its <c>EnvironmentCredential</c> link in the chain) will resolve to the
        /// Azure Key Vault Emulator's own token endpoint.
        ///
        /// <para>
        /// Sets the standard Azure SDK environment variables (<c>AZURE_TENANT_ID</c>,
        /// <c>AZURE_CLIENT_ID</c>, <c>AZURE_CLIENT_SECRET</c>, <c>AZURE_AUTHORITY_HOST</c>) on the
        /// consumer pointing at the emulator. Optionally, if <c>AZURE_TENANT_ID</c> is set on the
        /// host machine, its value is preferred so that the WWW-Authenticate challenge issued by the
        /// emulator advertises the user's real tenant. The emulator does not validate inbound tokens,
        /// so any token MSAL successfully acquires is accepted.
        /// </para>
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

            var hostTenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            var tenantId = string.IsNullOrWhiteSpace(hostTenantId) ? _emulatorTenantId : hostTenantId;

            return consumer.WithEnvironment(ctx =>
            {
                // We can't compute the emulator endpoint until allocation; resolve lazily inside the
                // callback so the URL is correct in every environment.
                var endpoint = emulator.Resource.Annotations
                    .OfType<EndpointAnnotation>()
                    .FirstOrDefault(a => a.UriScheme == "https" || a.Name == "https");

                var emulatorUrl = endpoint?.AllocatedEndpoint is { } allocated
                    ? allocated.UriString
                    : null;

                ctx.EnvironmentVariables["AZURE_TENANT_ID"] = tenantId;
                ctx.EnvironmentVariables["AZURE_CLIENT_ID"] = _emulatorClientId;
                ctx.EnvironmentVariables["AZURE_CLIENT_SECRET"] = _emulatorClientSecret;

                if (!string.IsNullOrWhiteSpace(emulatorUrl))
                    ctx.EnvironmentVariables["AZURE_AUTHORITY_HOST"] = emulatorUrl;
            });
        }
    }
}

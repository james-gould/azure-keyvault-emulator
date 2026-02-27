using Aspire.Hosting.Azure;
using Azure.Security.KeyVault.Secrets;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Certificates;
using AzureKeyVaultEmulator.Aspire.Hosting.Constants;
using AzureKeyVaultEmulator.Aspire.Hosting.Exceptions;
using AzureKeyVaultEmulator.Aspire.Hosting.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzureKeyVaultEmulator.Aspire.Hosting
{
    public static class KeyVaultEmulatorExtensions
    {
        private static List<KeyVaultSecret> _seedingSecrets = new();
        private static List<(string keyName, CreateKeyOptions options, KeyType type)> _seedingKeys = new();
        private static List<(string certificateName, CertificatePolicy policy)> _seedingCertificates = new();

        /// <summary>
        /// Directly adds the AzureKeyVaultEmulator as a container instead of routing through an Azure resource.
        /// </summary>
        /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the container to.</param>
        /// <param name="name">The name of the resource that will output as a connection string.</param>
        /// <param name="options">Optional granular configuration of the Azure Key Vault Emulator.</param>
        /// <returns>The original <paramref name="builder"/> updated to run the emulated Azure Key Vault.</returns>
        /// <exception cref="KeyVaultEmulatorException">When the <see cref="KeyVaultEmulatorOptions"/> is not valid.</exception>
        /// <exception cref="ArgumentNullException">When required parameters are null or defaulted.</exception>
        public static IResourceBuilder<AzureKeyVaultResource> AddAzureKeyVaultEmulator(
            this IDistributedApplicationBuilder builder,
            string name,
            KeyVaultEmulatorOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            options = builder.GetOrCreateConfigurationOptions(name, options);

            return builder
                    .AddAzureKeyVault(name)
                    .InnerAddEmulator(options);
        }

        /// <summary>
        ///  Run the <see cref="AzureKeyVaultResource"/> as a container locally.
        /// </summary>
        /// <param name="builder">The builder for the <see cref="AzureKeyVaultResource"/> resource.</param>
        /// <param name="options">Optional granular configuration of the Azure Key Vault Emulator.</param>
        /// <param name="configSectionName">Optional configuration section name to create <see cref="KeyVaultEmulatorOptions"/>.</param>
        /// <returns>The original <paramref name="builder"/> updated to run the emulated Azure Key Vault.</returns>
        /// <exception cref="KeyVaultEmulatorException">When the <see cref="KeyVaultEmulatorOptions"/> is not valid.</exception>
        /// <exception cref="ArgumentNullException">When required parameters are null or defaulted.</exception>
        public static IResourceBuilder<AzureKeyVaultResource> RunAsEmulator(
            this IResourceBuilder<AzureKeyVaultResource> builder,
            KeyVaultEmulatorOptions? options = null,
            string? configSectionName = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(builder.ApplicationBuilder);

            options = builder.ApplicationBuilder.GetOrCreateConfigurationOptions(configSectionName, options);

            return builder.InnerAddEmulator(options);
        }

        /// <summary>
        /// Overwrites the <see cref="AzureKeyVaultResource"/> to prevent provisioning and runs the Emulator container instance locally.
        /// </summary>
        /// <param name="builder">The builder for the <see cref="AzureKeyVaultResource"/> resource.</param>
        /// <param name="options">Optional granular configuration of the Azure Key Vault Emulator.</param>
        /// <returns>The original <paramref name="builder"/> updated to run the emulated Azure Key Vault.</returns>
        /// <exception cref="KeyVaultEmulatorException">When the <see cref="KeyVaultEmulatorOptions"/> is not valid.</exception>
        /// <exception cref="ArgumentNullException">When required parameters are null or defaulted.</exception>
        private static IResourceBuilder<AzureKeyVaultResource> InnerAddEmulator(
            this IResourceBuilder<AzureKeyVaultResource> builder,
            KeyVaultEmulatorOptions options)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(options);

            if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
                return builder;

            if (!options.IsValidCustomisable)
                throw new KeyVaultEmulatorException($"The configuration of {nameof(KeyVaultEmulatorOptions)} is not valid.");

            var hostCertificatePath = GetOrCreateLocalCertificates(options);

            ArgumentException.ThrowIfNullOrEmpty(hostCertificatePath);

            var containerTag = AzureKeyVaultEnvHelper.GetContainerTag();

            var keyVaultResourceBuilder = builder.ApplicationBuilder.CreateResourceBuilder(new AzureKeyVaultEmulatorResource(builder.Resource))
                   .WithImage(KeyVaultEmulatorContainerConstants.Image)
                   .WithImageRegistry(KeyVaultEmulatorContainerConstants.Registry)
                   .WithImageTag(containerTag)
                   .WithBindMount(
                        source: hostCertificatePath,
                        target: KeyVaultEmulatorCertConstants.CertMountTarget)
                    .WithLifetime(options.Lifetime)
                    .WithHttpsEndpoint(targetPort: KeyVaultEmulatorContainerConstants.Port)
                    .WithEnvironment(ctx =>
                    {
                        ctx.EnvironmentVariables.Add(KeyVaultEmulatorContainerConstants.PersistData, $"{options.Persist}");
                    })
                    .OnBeforeResourceStarted((emulator, resourceEvent, ct) =>
                    {
                        var endpoint = emulator.GetEndpoint("https");

                        if (string.IsNullOrEmpty(endpoint.Url))
                            throw new InvalidOperationException($"Failed to find endpoint URL for {nameof(AzureKeyVaultEmulatorResource)}");

                        if(builder.Resource.Outputs.Any())
                            builder.Resource.Outputs.Clear();

                        builder.Resource.Outputs.Add("vaultUri", endpoint.Url);

                        emulator.VaultUri = endpoint.Url;

                        return Task.CompletedTask;
                    })
                    .OnResourceReady(async (emulatedResource, resourceEvent, ct) =>
                    {
                        var secrets = resourceEvent.ExtractSecrets();

                        await MapSecretsToEmulatorAsync(emulatedResource.VaultUri, secrets);
                    })
                    .WithHttpHealthCheck("/token")
                    .WithAnnotation(new EmulatorResourceAnnotation());

            builder.MapResourceEvents(keyVaultResourceBuilder);

            return builder;
        }

        /// <summary>
        /// Adds a secret to be seeded into the specified Azure Key Vault Emulator resource at runtime.
        /// </summary>
        /// <remarks>This method registers the secret for seeding but does not immediately create it in
        /// Azure Key Vault. The secret will be created when the container is running in a healthy state.</remarks>
        /// <param name="keyVault">The resource builder for the Azure Key Vault to which the secret will be added. Cannot be null.</param>
        /// <param name="secretName">The name of the secret to add. Cannot be null.</param>
        /// <param name="secretValue">The value of the secret to add. Cannot be null.</param>
        /// <returns>The same resource builder instance for the Azure Key Vault, enabling method chaining.</returns>
        public static IResourceBuilder<AzureKeyVaultResource> SeedWithSecret(
            this IResourceBuilder<AzureKeyVaultResource> keyVault, string secretName, string secretValue)
        {
            ArgumentNullException.ThrowIfNull(keyVault);
            ArgumentNullException.ThrowIfNull(secretName);
            ArgumentNullException.ThrowIfNull(secretValue);

            var secret = new KeyVaultSecret(secretName, secretValue);

            _seedingSecrets.Add(secret);

            return keyVault;
        }

        /// <summary>
        /// Adds a key to be seeded into the specified Azure Key Vault Emulator resource at runtime.
        /// </summary>
        /// <remarks>This method registers the specified key to be created in the Key Vault when the
        /// resource is provisioned. It does not immediately create the key in Azure Key Vault; the key will be created
        /// when the container is running in a healthy state.</remarks>
        /// <param name="keyVault">The resource builder for the Azure Key Vault to which the key will be added. Cannot be null.</param>
        /// <param name="keyName">The name of the key to seed into the Key Vault. Cannot be null.</param>
        /// <param name="options">The options used to configure the creation of the key.</param>
        /// <param name="keyType">The type of key to create in the Key Vault.</param>
        /// <returns>The same resource builder instance for the Azure Key Vault, enabling method chaining.</returns>
        public static IResourceBuilder<AzureKeyVaultResource> SeedWithKey(
            this IResourceBuilder<AzureKeyVaultResource> keyVault, string keyName, CreateKeyOptions options, KeyType keyType)
        {
            ArgumentNullException.ThrowIfNull(keyVault);
            ArgumentNullException.ThrowIfNull(keyName);

            _seedingKeys.Add((keyName, options, keyType));

            return keyVault;
        }

        /// <summary>
        /// Adds a certificate to be seeded into the specified Azure Key Vault resource at runtime.
        /// </summary>
        /// <remarks>This method is typically used as part of a resource definition chain to ensure that
        /// the specified certificate is created in the Key Vault when the container is running in a healthy state.</remarks>
        /// <param name="keyVault">The resource builder for the Azure Key Vault to which the certificate will be added. Cannot be null.</param>
        /// <param name="certificateName">The name of the certificate to seed. Cannot be null.</param>
        /// <param name="policy">An optional certificate policy to apply when creating the certificate. If null, the default policy is used.</param>
        /// <returns>The original resource builder for the Azure Key Vault, enabling method chaining.</returns>
        public static IResourceBuilder<AzureKeyVaultResource> SeedWithCertificate(
            this IResourceBuilder<AzureKeyVaultResource> keyVault, string certificateName, CertificatePolicy? policy = null)
        {
            ArgumentNullException.ThrowIfNull(keyVault);
            ArgumentNullException.ThrowIfNull(certificateName);

            _seedingCertificates.Add((certificateName, policy ?? CertificatePolicy.Default));

            return keyVault;
        }

        /// <summary>
        /// Gets the directory for the local certificates, required to mount it into the Emulator container as a volume.
        /// </summary>
        /// <param name="options">The granular configuration of the Emulator.</param>
        /// <returns>The absolute path on the host machine, containing the required certificates to achieve valid, trusted SSL.</returns>
        private static string GetOrCreateLocalCertificates(KeyVaultEmulatorOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var certs = AzureKeyVaultEmulatorCertHelper.ValidateOrGenerateCertificate(options);

            if (options.LoadCertificatesIntoTrustStore)
                AzureKeyVaultEmulatorCertHelper.TryWriteToStore(options, certs.Pfx, certs.LocalCertificatePath, certs.pem);

            return certs.LocalCertificatePath;
        }

        /// <summary>
        /// Creates an instance of <see cref="KeyVaultEmulatorOptions"/> from either IConfiguration, direct instantsiation or defaults the values.
        /// </summary>
        /// <param name="builder">The builder for the <see cref="AzureKeyVaultResource"/> resource.</param>
        /// <param name="options">Optional granular configuration of the Azure Key Vault Emulator.</param>
        /// <param name="configSectionName">Optional configuration section name to create <see cref="KeyVaultEmulatorOptions"/>.</param>
        /// <returns></returns>
        private static KeyVaultEmulatorOptions GetOrCreateConfigurationOptions(
            this IDistributedApplicationBuilder builder,
            string? configSectionName = null,
            KeyVaultEmulatorOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            if (options is not null)
                return options;

            try
            {
                if (!string.IsNullOrEmpty(configSectionName))
                    options = builder.Configuration.GetSection(configSectionName).Get<KeyVaultEmulatorOptions>();
            }
            catch { }

            return options ?? new();
        }

        /// <summary>
        /// Configures event propagation from the Azure Key Vault resource to the associated Key Vault emulator resource
        /// within the application builder pipeline.
        /// </summary>
        /// <remarks>This method ensures that lifecycle events such as resource startup and readiness are
        /// forwarded from the Azure Key Vault resource to the emulator resource. This is useful for scenarios where
        /// emulator state must remain synchronized with the main resource during application initialization.</remarks>
        /// <param name="builder">The resource builder for the Azure Key Vault resource to which event subscriptions will be added.</param>
        /// <param name="keyVaultResourceBuilder">The resource builder for the Azure Key Vault emulator resource that will receive propagated events.</param>
        /// <returns>The original resource builder for the Azure Key Vault resource, enabling method chaining.</returns>
        private static IResourceBuilder<AzureKeyVaultResource> MapResourceEvents
            (this IResourceBuilder<AzureKeyVaultResource> builder, IResourceBuilder<AzureKeyVaultEmulatorResource> keyVaultResourceBuilder)
        {
            var eventing = builder.ApplicationBuilder.Eventing;

            eventing.Subscribe<BeforeResourceStartedEvent>(builder.Resource, async (resourceEvent, ct) =>
            {
                await eventing.PublishAsync(new BeforeResourceStartedEvent(keyVaultResourceBuilder.Resource, resourceEvent.Services), ct);
            });

            eventing.Subscribe<ResourceReadyEvent>(builder.Resource, async (resourceEvent, ct) =>
            {
                await eventing.PublishAsync(new ResourceReadyEvent(keyVaultResourceBuilder.Resource, resourceEvent.Services), ct);
            });

            return builder;
        }

        private static IEnumerable<AzureKeyVaultSecretResource> ExtractSecrets(this ResourceReadyEvent resourceEvent)
        {
            var model = resourceEvent.Services.GetRequiredService<DistributedApplicationModel>();

            return model.Resources.OfType<AzureKeyVaultSecretResource>() ?? [];
        }

        private static async ValueTask MapSecretsToEmulatorAsync(
            string vaultUri,
            IEnumerable<AzureKeyVaultSecretResource> secrets)
        {
            ArgumentException.ThrowIfNullOrEmpty(vaultUri);

            if (!secrets.Any())
                return;

            var client = AzureKeyVaultEmulatorClientHelper.GetSecretClient(vaultUri);

            var tasks = secrets.Select(s => SetSecretAsync(client, s));

            await Task.WhenAll(tasks);
        }

        private static async Task SetSecretAsync(SecretClient client, AzureKeyVaultSecretResource secretResource)
        {
            var param = secretResource.Value as ParameterResource
                ?? throw new KeyVaultEmulatorException($"Failed to cast secret {secretResource.Name} to {typeof(ParameterResource)}");

            var value = await param.GetValueAsync(default);

            await client.SetSecretAsync(secretResource.SecretName, value);
        }

        private class AzureKeyVaultEmulatorResource(AzureKeyVaultResource resource) : ContainerResource(resource.Name)
        {
            public override ResourceAnnotationCollection Annotations => resource.Annotations;

            internal string VaultUri { get; set; } = string.Empty;

        }
    }
}

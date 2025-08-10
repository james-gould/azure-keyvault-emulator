using Aspire.Hosting.Azure;
using AzureKeyVaultEmulator.Aspire.Hosting.Constants;
using AzureKeyVaultEmulator.Aspire.Hosting.Exceptions;
using AzureKeyVaultEmulator.Aspire.Hosting.Helpers;
using Microsoft.Extensions.Configuration;

namespace AzureKeyVaultEmulator.Aspire.Hosting
{
    public static class KeyVaultEmulatorExtensions
    {
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

            var keyVaultResourceBuilder = builder.ApplicationBuilder.CreateResourceBuilder(new AzureKeyVaultEmulatorResource(builder.Resource))
                   .WithImage(KeyVaultEmulatorContainerConstants.Image)
                   .WithImageRegistry(KeyVaultEmulatorContainerConstants.Registry)
                   .WithImageTag(KeyVaultEmulatorContainerConstants.Tag)
                   .WithBindMount(
                        source: hostCertificatePath,
                        target: KeyVaultEmulatorCertConstants.CertMountTarget)
                    .WithLifetime(options.Lifetime)
                    .WithHttpsEndpoint(targetPort: KeyVaultEmulatorContainerConstants.Port)
                    .WithEnvironment(ctx =>
                    {
                        ctx.EnvironmentVariables.Add(KeyVaultEmulatorContainerConstants.PersistData, $"{options.Persist}");
                    })
                    .OnResourceEndpointsAllocated((emulator, resourceEvent, ct) =>
                    {
                        var t = emulator.GetEndpoints();
                        var endpoint = emulator.GetEndpoint("https");

                        builder.Resource.Outputs.Add("vaultUri", endpoint.Url);

                        return Task.CompletedTask;
                    })
                    .WithHttpHealthCheck("/token")
                    .WithAnnotation(new EmulatorResourceAnnotation());

            // We need to forward events from the real Azure Key Vault resource to the emulator resource
            var eventing = builder.ApplicationBuilder.Eventing;

            eventing.Subscribe<BeforeResourceStartedEvent>(builder.Resource, async (resourceEvent, ct) =>
            {
                await eventing.PublishAsync(new BeforeResourceStartedEvent(keyVaultResourceBuilder.Resource, resourceEvent.Services), ct);
            });

            eventing.Subscribe<ResourceEndpointsAllocatedEvent>(builder.Resource, async (resourceEvent, ct) =>
            {
                await eventing.PublishAsync(new ResourceEndpointsAllocatedEvent(keyVaultResourceBuilder.Resource, resourceEvent.Services), ct);
            });

            keyVaultResourceBuilder.ApplicationBuilder.Eventing.Subscribe<ResourceEndpointsAllocatedEvent>((resource, ct) =>
            {
                var t = resource.Resource.TryGetEndpoints(out var endpointCollection);

                return Task.CompletedTask;
            });

            // Something has to be set before the endpoint is available to ensure the Bicep validation passes
            builder.Resource.Outputs.Add("vaultUri", "https://localhost:7994");

            return builder;
        }

        /// <summary>
        /// Gets the directory for the local certificates, required to mount it into the Emulator container as a volume.
        /// </summary>
        /// <param name="options">The granular configuration of the Emulator.</param>
        /// <returns>The absolute path on the host machine, containing the required certificates to achieve valid, trusted SSL.</returns>
        private static string GetOrCreateLocalCertificates(KeyVaultEmulatorOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var certs = KeyVaultEmulatorCertHelper.ValidateOrGenerateCertificate(options);

            if (options.LoadCertificatesIntoTrustStore)
                KeyVaultEmulatorCertHelper.TryWriteToStore(options, certs.Pfx, certs.LocalCertificatePath, certs.pem);

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

        private class AzureKeyVaultEmulatorResource(AzureKeyVaultResource resource) : ContainerResource(resource.Name)
        {
            public override ResourceAnnotationCollection Annotations => resource.Annotations;
        }
    }
}

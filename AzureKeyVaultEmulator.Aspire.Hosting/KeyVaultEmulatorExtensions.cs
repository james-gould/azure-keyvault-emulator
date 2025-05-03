using Aspire.Hosting.Azure;
using AzureKeyVaultEmulator.Aspire.Hosting.Constants;
using AzureKeyVaultEmulator.Aspire.Hosting.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Sockets;

namespace AzureKeyVaultEmulator.Aspire.Hosting
{
    public static class KeyVaultEmulatorExtensions
    {
        /// <summary>
        /// Directly adds the AzureKeyVaultEmulator as a container instead of routing through an Azure resource.
        /// </summary>
        /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the container to.</param>
        /// <param name="name">The name of the resource that will output as a connection string.</param>
        /// <param name="lifetime">Sets the <see cref="ContainerLifetime"/> of the Azure Key Vault Emulator container, allowing for desired destruction of secure data on shutdown.</param>
        /// <param name="options">Optional granular configuration of the Azure Key Vault Emulator.</param>
        /// <param name="configSectionName">Optional configuration section name to create <see cref="KeyVaultEmulatorConfiguration"/>.</param>
        /// <returns>The original <paramref name="builder"/> updated to run the emulated Azure Key Vault.</returns>
        /// <exception cref="KeyVaultEmulatorException">When the <see cref="KeyVaultEmulatorConfiguration"/> is not valid.</exception>
        /// <exception cref="ArgumentNullException">When required parameters are null or defaulted.</exception>
        public static IResourceBuilder<AzureKeyVaultResource> AddAzureKeyVaultEmulator(
            this IDistributedApplicationBuilder builder,
            [ResourceName] string name,
            ContainerLifetime lifetime = ContainerLifetime.Session,
            KeyVaultEmulatorConfiguration? options = null,
            string? configSectionName = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            options = builder.GetOrCreateConfigurationOptions(configSectionName, options);

            return builder
                    .AddAzureKeyVault(name)
                    .InnerAddEmulator(options, lifetime);
        }

        /// <summary>
        ///  Run the <see cref="AzureKeyVaultResource"/> as a container locally.
        /// </summary>
        /// <param name="builder">The builder for the <see cref="AzureKeyVaultResource"/> resource.</param>
        /// <param name="lifetime">Configures the <see cref="ContainerLifetime"/> of the emulator container, defaulted as <see cref="ContainerLifetime.Session"/>.</param>
        /// <param name="options">Optional granular configuration of the Azure Key Vault Emulator.</param>
        /// <param name="configSectionName">Optional configuration section name to create <see cref="KeyVaultEmulatorConfiguration"/>.</param>
        /// <returns>The original <paramref name="builder"/> updated to run the emulated Azure Key Vault.</returns>
        /// <exception cref="KeyVaultEmulatorException">When the <see cref="KeyVaultEmulatorConfiguration"/> is not valid.</exception>
        /// <exception cref="ArgumentNullException">When required parameters are null or defaulted.</exception>
        public static IResourceBuilder<AzureKeyVaultResource> RunAsEmulator(
            this IResourceBuilder<AzureKeyVaultResource> builder,
            ContainerLifetime lifetime = ContainerLifetime.Session,
            KeyVaultEmulatorConfiguration? options = null,
            string? configSectionName = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(builder.ApplicationBuilder);

            options = builder.ApplicationBuilder.GetOrCreateConfigurationOptions(configSectionName, options);

            return builder.InnerAddEmulator(options, lifetime);
        }

        /// <summary>
        /// Overwrites the <see cref="AzureKeyVaultResource"/> to prevent provisioning and runs the Emulator container instance locally.
        /// </summary>
        /// <param name="builder">The builder for the <see cref="AzureKeyVaultResource"/> resource.</param>
        /// <param name="lifetime">Configures the <see cref="ContainerLifetime"/> of the emulator container, defaulted as <see cref="ContainerLifetime.Session"/>.</param>
        /// <param name="options">Optional granular configuration of the Azure Key Vault Emulator.</param>
        /// <returns>The original <paramref name="builder"/> updated to run the emulated Azure Key Vault.</returns>
        /// <exception cref="KeyVaultEmulatorException">When the <see cref="KeyVaultEmulatorConfiguration"/> is not valid.</exception>
        /// <exception cref="ArgumentNullException">When required parameters are null or defaulted.</exception>
        private static IResourceBuilder<AzureKeyVaultResource> InnerAddEmulator(
            this IResourceBuilder<AzureKeyVaultResource> builder,
            KeyVaultEmulatorConfiguration options,
            ContainerLifetime lifetime = ContainerLifetime.Session)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(options);

            if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
                return builder;

            if (!options.IsValidCustomisable)
                throw new KeyVaultEmulatorException($"The configuration of {nameof(KeyVaultEmulatorConfiguration)} is not valid.");

            var hostCertificatePath = GetLocalCertificatePath(options);

            ArgumentException.ThrowIfNullOrEmpty(hostCertificatePath);

            builder
                .WithAnnotation(new ContainerImageAnnotation
                {
                    Registry = KeyVaultEmulatorContainerConstants.Registry,
                    Image = KeyVaultEmulatorContainerConstants.Image,
                    Tag = KeyVaultEmulatorContainerConstants.Tag,
                })
                .WithAnnotation(new ContainerMountAnnotation(
                    source: hostCertificatePath,
                    target: KeyVaultEmulatorCertConstants.CertMountTarget,
                    type: ContainerMountType.BindMount,
                    isReadOnly: true))
                .WithAnnotation(new ContainerLifetimeAnnotation { Lifetime = lifetime })
                .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp)
                {
                    Port = KeyVaultEmulatorContainerConstants.Port,
                    TargetPort = KeyVaultEmulatorContainerConstants.Port,
                    UriScheme = "https",
                    Name = "https"
                });

            builder.Resource.Outputs.Add("vaultUri", KeyVaultEmulatorContainerConstants.Endpoint);

            builder.RegisterOptionalLifecycleHandler(options, hostCertificatePath);

            return builder;
        }

        /// <summary>
        /// Gets the directory for the local certificates, required to mount it into the Emulator container as a volume.
        /// </summary>
        /// <param name="options">The granular configuration of the Emulator.</param>
        /// <returns>The absolute path on the host machine, containing the required certificates to achieve valid, trusted SSL.</returns>
        private static string GetLocalCertificatePath(KeyVaultEmulatorConfiguration options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return options.ShouldGenerateCertificates
                    ? KeyVaultEmulatorCertHelper.GetConfigurableCertStoragePath(options.LocalCertificatePath)
                    : options.LocalCertificatePath;
        }

        /// <summary>
        /// if <see cref="KeyVaultEmulatorConfiguration.ForceCleanupOnShutdown"/> toggled on, register an instance of <see cref="KeyVaultEmulatorLifecycleService"/>
        /// </summary>
        /// <param name="builder">The builder being overridden.</param>
        /// <param name="options">The granular options for the Azure Key Vault Emulator.</param>
        /// <param name="hostMachineCertificatePath">The certificate path, provided by <see cref="GetLocalCertificatePath(KeyVaultEmulatorConfiguration)"/></param>
        private static void RegisterOptionalLifecycleHandler(
            this IResourceBuilder<AzureKeyVaultResource> builder,
            KeyVaultEmulatorConfiguration options,
            string hostMachineCertificatePath)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrEmpty(hostMachineCertificatePath);

            if (options.ForceCleanupOnShutdown)
                builder.ApplicationBuilder.Services.AddHostedService(
                    provider => new KeyVaultEmulatorLifecycleService(hostMachineCertificatePath));
        }

        /// <summary>
        /// Creates an instance of <see cref="KeyVaultEmulatorConfiguration"/> from either IConfiguration, direct instantsiation or defaults the values.
        /// </summary>
        /// <param name="builder">The builder for the <see cref="AzureKeyVaultResource"/> resource.</param>
        /// <param name="options">Optional granular configuration of the Azure Key Vault Emulator.</param>
        /// <param name="configSectionName">Optional configuration section name to create <see cref="KeyVaultEmulatorConfiguration"/>.</param>
        /// <returns></returns>
        private static KeyVaultEmulatorConfiguration GetOrCreateConfigurationOptions(
            this IDistributedApplicationBuilder builder,
            string? configSectionName = null,
            KeyVaultEmulatorConfiguration? options = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            if (options is not null)
                return options;

            try
            {
                if (!string.IsNullOrEmpty(configSectionName))
                    options = builder.Configuration.GetSection(configSectionName).Get<KeyVaultEmulatorConfiguration>();
            }
            catch { }

            return options ?? new();
        }
    }
}

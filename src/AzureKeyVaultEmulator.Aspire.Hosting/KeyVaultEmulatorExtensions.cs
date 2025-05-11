using System.Net.Sockets;
using Aspire.Hosting.Azure;
using AzureKeyVaultEmulator.Aspire.Hosting.Constants;
using AzureKeyVaultEmulator.Aspire.Hosting.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
//using System.Net.Sockets;

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
                    isReadOnly: false))
                .WithAnnotation(new ContainerLifetimeAnnotation { Lifetime = options.Lifetime })
                .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp)
                {
                    Port = KeyVaultEmulatorContainerConstants.Port,
                    TargetPort = KeyVaultEmulatorContainerConstants.Port,
                    UriScheme = "https",
                    Name = "https"
                })
                .WithAnnotation(
                    new EnvironmentCallbackAnnotation(ctx => RegisterEnvironmentVariables(ctx, options))
                );

            builder.Resource.Outputs.Add("vaultUri", KeyVaultEmulatorContainerConstants.Endpoint);

            builder.RegisterOptionalLifecycleHandler(options, hostCertificatePath);

            return builder;
        }

        /// <summary>
        /// Registers the necessary Environment Variables for the container runtime.
        /// </summary>
        /// <param name="context">The environment context for execution.</param>
        /// <param name="options">The options defined for the emulator.</param>
        /// <returns>The context with the EnvironmentVariables extended.</returns>
        private static EnvironmentCallbackContext RegisterEnvironmentVariables(
            EnvironmentCallbackContext context,
            KeyVaultEmulatorOptions options)
        {
            context.EnvironmentVariables.Add(KeyVaultEmulatorContainerConstants.PersistData, options.Persist.ToString());

            return context;
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
        /// if <see cref="KeyVaultEmulatorOptions.ForceCleanupOnShutdown"/> toggled on, register an instance of <see cref="KeyVaultEmulatorLifecycleService"/>
        /// </summary>
        /// <param name="builder">The builder being overridden.</param>
        /// <param name="options">The granular options for the Azure Key Vault Emulator.</param>
        /// <param name="hostMachineCertificatePath">The certificate path, provided by <see cref="GetOrCreateLocalCertificates(KeyVaultEmulatorOptions)"/></param>
        private static void RegisterOptionalLifecycleHandler(
            this IResourceBuilder<AzureKeyVaultResource> builder,
            KeyVaultEmulatorOptions options,
            string hostMachineCertificatePath)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrEmpty(hostMachineCertificatePath);

            if (options.ForceCleanupOnShutdown)
                builder.ApplicationBuilder.Services.AddHostedService(provider =>
                {
                    var lifetime = provider.GetService<IHostApplicationLifetime>();

                    return new KeyVaultEmulatorLifecycleService(hostMachineCertificatePath, lifetime);
                });
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
    }
}

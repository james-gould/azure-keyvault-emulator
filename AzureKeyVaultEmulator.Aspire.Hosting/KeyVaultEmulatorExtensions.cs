using Aspire.Hosting.Azure;
using Azure.Provisioning.KeyVault;
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
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public static IResourceBuilder<AzureKeyVaultResource> AddAzureKeyVaultEmulator(
            this IDistributedApplicationBuilder builder,
            [ResourceName] string name,
            ContainerLifetime lifetime = ContainerLifetime.Session)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            return builder
                    .AddAzureKeyVault(name)
                    .RunAsEmulator(lifetime);
        }

        /// <summary>
        ///  Run the <see cref="AzureKeyVaultResource"/> as a container locally.
        /// </summary>
        /// <param name="builder">The builder for the <see cref="AzureKeyVaultResource"/> resource.</param>
        /// <param name="lifetime">Configures the <see cref="ContainerLifetime"/> of the emulator container, defaulted as <see cref="ContainerLifetime.Session"/>.</param>
        /// <returns>The original <paramref name="builder"/> updated to run the emulated Azure Key Vault.</returns>
        public static IResourceBuilder<AzureKeyVaultResource> RunAsEmulator(
            this IResourceBuilder<AzureKeyVaultResource> builder,
            ContainerLifetime lifetime = ContainerLifetime.Session)
        {
            ArgumentNullException.ThrowIfNull(builder);

            if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
                return builder;

            builder
                .WithAnnotation(new ContainerImageAnnotation
                {
                    Registry = KeyVaultEmulatorConstants.Registry,
                    Image = KeyVaultEmulatorConstants.Image,
                    Tag = KeyVaultEmulatorConstants.Tag,
                })
                .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp)
                {
                    Port = KeyVaultEmulatorConstants.Port,
                    TargetPort = KeyVaultEmulatorConstants.Port,
                    UriScheme = "https",
                    Name = "https"
                })
                .WithAnnotation(new ContainerLifetimeAnnotation { Lifetime = lifetime });

            builder.Resource.Outputs.Add("vaultUri", KeyVaultEmulatorConstants.Endpoint);

            return builder;
        }

        /// <summary>
        /// <para>Implements the existing extension method for the <see cref="AzureKeyVaultResource"/>.</para>
        /// <para>Does not actually create role assignments, simply prevents build issues when opting for the emulator!</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The resource to which the specified roles will be assigned.</param>
        /// <param name="roles">The built-in Key Vault roles to be assigned.</param>
        /// <returns>The UNCHANGED <see cref="IResourceBuilder{T}"/> with no role assignments created.</returns>
        public static IResourceBuilder<AzureKeyVaultResource> WithRoleAssignments<T>(
            this IResourceBuilder<AzureKeyVaultResource> builder,
            params KeyVaultBuiltInRole[] roles)
            where T : IResource
                => builder;
    }
}

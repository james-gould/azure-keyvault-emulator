using Aspire.Hosting.Azure;
using Azure.Provisioning.KeyVault;
using AzureKeyVaultEmulator.Aspire.Hosting;

namespace AzureKeyVaultEmulator.Hosting.Aspire
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
        public static IResourceBuilder<KeyVaultDirectEmulatorResource> AddAzureKeyVaultEmulator(
            this IDistributedApplicationBuilder builder,
            [ResourceName] string name,
            ContainerLifetime lifetime = ContainerLifetime.Session)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var resource = new KeyVaultDirectEmulatorResource(name);

            return builder
                .AddResource(resource)
                .WithImage(KeyVaultEmulatorContainerImageTags.Image)
                .WithImageTag(KeyVaultEmulatorContainerImageTags.Tag)
                .WithImageRegistry(KeyVaultEmulatorContainerImageTags.Registry)
                .WithHttpsEndpoint(
                    name: KeyVaultEmulatorContainerImageTags.Name,
                    port: KeyVaultEmulatorContainerImageTags.Port,
                    targetPort: KeyVaultEmulatorContainerImageTags.Port)
                .WithLifetime(lifetime);
        }

        /// <summary>
        ///  Configures a container to run the AzureKeyVaultEmulator application, overwriting the <see cref="AzureKeyVaultResource"/> builder.
        /// </summary>
        /// <param name="builder">The original builder for the <see cref="AzureKeyVaultResource"/> resource.</param>
        /// <param name="configureContainer">Provides the ability to configure the container as you need it to run.</param>
        /// <returns>A new <see cref="IResourceBuilder{T}"/></returns>
        public static IResourceBuilder<KeyVaultEmulatorResource> RunAsEmulator(
            this IResourceBuilder<AzureKeyVaultResource> builder,
            Action<IResourceBuilder<KeyVaultEmulatorResource>>? configureContainer = null)
        {
            var emulatedResource = new KeyVaultEmulatorResource(builder.Resource);
            var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(emulatedResource);

            surrogateBuilder
                .WithAnnotation(new ContainerImageAnnotation
                {
                    Image = KeyVaultEmulatorContainerImageTags.Image,
                    Registry = KeyVaultEmulatorContainerImageTags.Registry,
                    Tag = KeyVaultEmulatorContainerImageTags.Tag
                })
                .WithHttpsEndpoint(
                    name: KeyVaultEmulatorContainerImageTags.Name,
                    port: KeyVaultEmulatorContainerImageTags.Port,
                    targetPort: KeyVaultEmulatorContainerImageTags.Port
                )
                .WithUrl(emulatedResource.ConnectionStringExpression);

            configureContainer?.Invoke(surrogateBuilder);

            return surrogateBuilder;
        }

        /// <summary>
        /// Provides the baseline KeyVault emulator with a specified <see cref="ContainerLifetime"/>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public static IResourceBuilder<KeyVaultEmulatorResource> RunAsEmulator(
            this IResourceBuilder<AzureKeyVaultResource> builder,
            ContainerLifetime lifetime)
        {
            var emulatedBuilder = RunAsEmulator(builder, null);

            emulatedBuilder.WithLifetime(lifetime);

            return emulatedBuilder;
        }

        /// <summary>
        /// Implements the existing extension method for the <see cref="AzureKeyVaultResource"/>. <br />
        /// Does not actually create role assignments, simply prevents build issues when opting for the emulator!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The resource to which the specified roles will be assigned.</param>
        /// <param name="target">The target Azure Key Vault resource.</param>
        /// <param name="roles">The built-in Key Vault roles to be assigned.</param>
        /// <returns>The UNCHANGED <see cref="IResourceBuilder{T}"/> with no role assignments created.</returns>
        public static IResourceBuilder<T> WithRoleAssignments<T>(
            this IResourceBuilder<T> builder,
            IResourceBuilder<KeyVaultEmulatorResource> target,
            params KeyVaultBuiltInRole[] roles)
            where T : IResource
                => builder;
    }
}

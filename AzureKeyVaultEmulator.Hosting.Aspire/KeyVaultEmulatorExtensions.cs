using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning.KeyVault;

namespace AzureKeyVaultEmulator.Hosting.Aspire
{
    public static class KeyVaultEmulatorExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureContainer"></param>
        /// <returns></returns>
        public static IResourceBuilder<KeyVaultEmulatorResource> RunAsEmulator(
            this IResourceBuilder<AzureKeyVaultResource> builder,
            Action<IResourceBuilder<KeyVaultEmulatorResource>>? configureContainer = null)
        {
            var emulatedResource = new KeyVaultEmulatorResource(builder.Resource);
            var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(emulatedResource);

            surrogateBuilder
                //.WithHttpsEndpoint(
                //    name: KeyVaultEmulatorContainerImageTags.Name,
                //    port: KeyVaultEmulatorContainerImageTags.Port,
                //    targetPort: KeyVaultEmulatorContainerImageTags.Port
                //)
                .WithEndpoint("emulatedEndpoint", x =>
                {
                    x.UriScheme = "https";
                    x.TargetHost = "emulator.azure.vault.net";
                    x.TargetPort = KeyVaultEmulatorContainerImageTags.Port;
                    x.Port = KeyVaultEmulatorContainerImageTags.Port;
                    x.Name = KeyVaultEmulatorContainerImageTags.Name;
                })
                .WithAnnotation(new ContainerImageAnnotation
                {
                    Image = KeyVaultEmulatorContainerImageTags.Image,
                    Tag = KeyVaultEmulatorContainerImageTags.Tag,
                    Registry = KeyVaultEmulatorContainerImageTags.Registry
                });

            configureContainer?.Invoke(surrogateBuilder);

            return surrogateBuilder;
        }

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
        {
            return builder;
        }
    }
}

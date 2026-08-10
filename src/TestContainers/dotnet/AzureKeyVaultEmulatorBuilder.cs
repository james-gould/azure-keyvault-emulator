using System;
using System.Diagnostics.CodeAnalysis;
using AzureKeyVaultEmulator.TestContainers.Constants;
using AzureKeyVaultEmulator.TestContainers.Helpers;
using AzureKeyVaultEmulator.TestContainers.Models;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Images;

namespace AzureKeyVaultEmulator.TestContainers
{
    /// <inheritdoc cref="ContainerBuilder{TBuilderEntity,TContainerEntity,TConfigurationEntity}" />
    public sealed class AzureKeyVaultEmulatorBuilder : ContainerBuilder<AzureKeyVaultEmulatorBuilder,
        AzureKeyVaultEmulatorContainer, AzureKeyVaultEmulatorConfiguration>
    {
        [Obsolete(
            "This constant is obsolete and will be removed in the future. Use the constructor with the image parameter instead: https://github.com/testcontainers/testcontainers-dotnet/discussions/1470#discussioncomment-15185721.")]
        public const string AzureKeyVaultEmulatorImage = "jamesgoulddev/azure-keyvault-emulator:3.1.3";

        public const ushort AzureKeyVaultEmulatorPort = 4997;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureKeyVaultEmulatorBuilder" /> class.
        /// </summary>
        [Obsolete(
            "This parameterless constructor is obsolete and will be removed. Use the constructor with the image parameter instead: https://github.com/testcontainers/testcontainers-dotnet/discussions/1470#discussioncomment-15185721.")]
        [ExcludeFromCodeCoverage]
        public AzureKeyVaultEmulatorBuilder() : this(AzureKeyVaultEmulatorImage)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureKeyVaultEmulatorBuilder" /> class.
        /// </summary>
        /// <param name="image">
        ///     The full Docker image name, including the image repository and tag
        ///     (e.g., <c>jamesgoulddev/azure-keyvault-emulator:3.1.3</c>).
        /// </param>
        /// <remarks>
        ///     Docker image tags available at <see href="https://hub.docker.com/r/jamesgoulddev/azure-keyvault-emulator/tags" />.
        /// </remarks>
        public AzureKeyVaultEmulatorBuilder(string image) : this(new DockerImage(image))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureKeyVaultEmulatorBuilder" /> class.
        /// </summary>
        /// <param name="image">
        ///     An <see cref="IImage" /> instance that specifies the Docker image to be used
        ///     for the container builder configuration.
        /// </param>
        /// <remarks>
        ///     Docker image tags available at <see href="https://hub.docker.com/r/jamesgoulddev/azure-keyvault-emulator/tags" />.
        /// </remarks>
        public AzureKeyVaultEmulatorBuilder(IImage image) : this(new AzureKeyVaultEmulatorConfiguration()) =>
            DockerResourceConfiguration = Init().WithImage(image).DockerResourceConfiguration;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureKeyVaultEmulatorBuilder" /> class.
        /// </summary>
        /// <param name="resourceConfiguration">The Docker resource configuration.</param>
        private AzureKeyVaultEmulatorBuilder(AzureKeyVaultEmulatorConfiguration resourceConfiguration) : base(
            resourceConfiguration) =>
            DockerResourceConfiguration = resourceConfiguration;

        /// <inheritdoc />
        protected override AzureKeyVaultEmulatorConfiguration DockerResourceConfiguration { get; }

        /// <inheritdoc />
        protected override AzureKeyVaultEmulatorBuilder Init() =>
            base.Init()
                .WithPortBinding(AzureKeyVaultEmulatorPort, true)
                .WithCertificates()
                .WithPersistence()
                .WithConnectionStringProvider(new AzureKeyVaultEmulatorConnectionStringProvider())
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(AzureKeyVaultEmulatorPort));

        /// <inheritdoc />
        public override AzureKeyVaultEmulatorContainer Build()
        {
            Validate();
            return new AzureKeyVaultEmulatorContainer(DockerResourceConfiguration);
        }

        /// <inheritdoc />
        protected override AzureKeyVaultEmulatorBuilder
            Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration) => Merge(
            DockerResourceConfiguration,
            new AzureKeyVaultEmulatorConfiguration(resourceConfiguration));

        /// <inheritdoc />
        protected override AzureKeyVaultEmulatorBuilder Merge(AzureKeyVaultEmulatorConfiguration oldValue,
            AzureKeyVaultEmulatorConfiguration newValue) =>
            new AzureKeyVaultEmulatorBuilder(new AzureKeyVaultEmulatorConfiguration(oldValue, newValue));

        /// <inheritdoc />
        protected override AzureKeyVaultEmulatorBuilder Clone(IContainerConfiguration resourceConfiguration) =>
            Merge(DockerResourceConfiguration, new AzureKeyVaultEmulatorConfiguration(resourceConfiguration));

        /// <summary>
        ///     Allows the Emulator to persist data beyond temporary storage for multi-session use.
        /// </summary>
        /// <param name="persist">Whether the Emulator should persist data beyond temporary storage.</param>
        /// <returns>A configured instance of <see cref="AzureKeyVaultEmulatorBuilder" />.</returns>
        public AzureKeyVaultEmulatorBuilder WithPersistence(bool persist = false) =>
            Merge(DockerResourceConfiguration, new AzureKeyVaultEmulatorConfiguration(persist: persist))
                .WithEnvironment(AzureKeyVaultEmulatorContainerConstants.PersistData,
                    persist.ToString().ToLowerInvariant());

        /// <summary>
        ///     Configures the SSL certificates for the Azure Key Vault Emulator container.
        /// </summary>
        /// <param name="certificatesDirectory">The local path to the directory containing the certificates.</param>
        /// <param name="generateCertificates">Whether to automatically generate SSL certificates if they don't exist.</param>
        /// <param name="forceCleanupCertificates">Whether to clean up the generated SSL certificates on application shutdown.</param>
        /// <param name="loadCertificatesIntoTrustStore">Whether to load the certificates into the host machine's trust store.</param>
        /// <returns>A configured instance of <see cref="AzureKeyVaultEmulatorBuilder" />.</returns>
        public AzureKeyVaultEmulatorBuilder WithCertificates(
            string? certificatesDirectory = null,
            bool generateCertificates = true,
            bool forceCleanupCertificates = false,
            bool loadCertificatesIntoTrustStore = true)
        {
            AzureKeyVaultEmulatorConfiguration config = new AzureKeyVaultEmulatorConfiguration(certificatesDirectory,
                generateCertificates: generateCertificates,
                forceCleanupCertificates: forceCleanupCertificates,
                loadCertificatesIntoTrustStore: loadCertificatesIntoTrustStore);

            CertificateLoaderVM loadedCertificates =
                AzureKeyVaultEmulatorCertHelper.ValidateOrGenerateCertificate(config);
            config.PFX = loadedCertificates.Pfx;

            if (loadCertificatesIntoTrustStore)
            {
                AzureKeyVaultEmulatorCertHelper.TryWriteToStore(DockerResourceConfiguration, loadedCertificates.Pfx,
                    loadedCertificates.LocalCertificatePath, loadedCertificates.pem);
            }

            return Merge(DockerResourceConfiguration, config)
                .WithBindMount(loadedCertificates.LocalCertificatePath,
                    AzureKeyVaultEmulatorCertConstants.CertMountTarget);
        }
    }
}

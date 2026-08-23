using System.Security.Cryptography.X509Certificates;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace AzureKeyVaultEmulator.TestContainers
{
    /// <inheritdoc cref="ContainerConfiguration" />
    public sealed class AzureKeyVaultEmulatorConfiguration : ContainerConfiguration
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureKeyVaultEmulatorConfiguration" /> class.
        /// </summary>
        /// <param name="certificatesDirectory">
        ///     The optional host directory containing SSL certificates. If not provided the
        ///     certificate will be generated to your User profile.
        /// </param>
        /// <param name="persist">Whether to enable data persistence.</param>
        /// <param name="generateCertificates">Whether to automatically generate SSL certificates if they don't exist.</param>
        /// <param name="forceCleanupCertificates">Uninstall the SSL certificates for the container on shutdown.</param>
        /// <param name="loadCertificatesIntoTrustStore">Whether to load the SSL certificates into the trust store.</param>
        public AzureKeyVaultEmulatorConfiguration(
            string? certificatesDirectory = null,
            bool persist = false,
            bool generateCertificates = true,
            bool forceCleanupCertificates = false,
            bool loadCertificatesIntoTrustStore = true)
        {
            Persist = persist;
            LocalCertificatePath = certificatesDirectory ?? string.Empty;
            ShouldGenerateCertificates = generateCertificates;
            ForceCleanupOnShutdown = forceCleanupCertificates;
            LoadCertificatesIntoTrustStore = loadCertificatesIntoTrustStore;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureKeyVaultEmulatorConfiguration" /> class.
        /// </summary>
        /// <param name="resourceConfiguration">The Docker resource configuration.</param>
        public AzureKeyVaultEmulatorConfiguration(IContainerConfiguration resourceConfiguration) : base(
            resourceConfiguration)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureKeyVaultEmulatorConfiguration" /> class.
        /// </summary>
        /// <param name="oldValue">The old Docker resource configuration.</param>
        /// <param name="newValue">The new Docker resource configuration.</param>
        public AzureKeyVaultEmulatorConfiguration(AzureKeyVaultEmulatorConfiguration oldValue,
            AzureKeyVaultEmulatorConfiguration newValue) : base(oldValue, newValue)
        {
            Persist = BuildConfiguration.Combine(oldValue.Persist, newValue.Persist);
            LocalCertificatePath =
                BuildConfiguration.Combine(oldValue.LocalCertificatePath, newValue.LocalCertificatePath);
            ShouldGenerateCertificates = BuildConfiguration.Combine(oldValue.ShouldGenerateCertificates,
                newValue.ShouldGenerateCertificates);
            ForceCleanupOnShutdown =
                BuildConfiguration.Combine(oldValue.ForceCleanupOnShutdown, newValue.ForceCleanupOnShutdown);
            LoadCertificatesIntoTrustStore = BuildConfiguration.Combine(oldValue.LoadCertificatesIntoTrustStore,
                newValue.LoadCertificatesIntoTrustStore);
            PFX = BuildConfiguration.Combine(oldValue.PFX, newValue.PFX);
            CRT = BuildConfiguration.Combine(oldValue.CRT, newValue.CRT);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureKeyVaultEmulatorConfiguration" /> class.
        /// </summary>
        /// <param name="resourceConfiguration">The Docker resource configuration.</param>
        public AzureKeyVaultEmulatorConfiguration(
            IResourceConfiguration<CreateContainerParameters> resourceConfiguration) :
            base(resourceConfiguration)
        {
        }

        /// <summary>
        ///     Allows the Emulator to persist data beyond temporary storage for multi-session use.
        /// </summary>
        public bool Persist { get; }

        /// <summary>
        ///     <para>Specify the directory to be used as a mount for the Azure Key Vault Emulator.</para>
        ///     <para>Warning: your container runtime must have read access to this directory.</para>
        /// </summary>
        public string LocalCertificatePath { get; } = string.Empty;

        /// <summary>
        ///     <para>Determines if the Emulator should attempt to load the certificates into the host machine's trust store.</para>
        ///     <para>Warning: this requires Administration rights.</para>
        ///     <para>Unused if the certificates are already present, removing the administration privilege requirement.</para>
        /// </summary>
        public bool LoadCertificatesIntoTrustStore { get; } = true;

        /// <summary>
        ///     <para>Disables the Azure Key Vault Emulator creating a self signed SSL certificate for you at runtime.</para>
        ///     <para>
        ///         Using this option will require you to provide a certificate in PFX (and optionally a CRT) format within the
        ///         same directory.
        ///         The directory must also be set via <see cref="LocalCertificatePath" />.
        ///     </para>
        ///     <para>
        ///         The PFX password MUST be "emulator" - all lowercase without the double quotes. This limitation is being
        ///         looked into.
        ///     </para>
        /// </summary>
        public bool ShouldGenerateCertificates { get; } = true;

        /// <summary>
        ///     <para>Cleans up the generated SSL certificates on application shutdown.</para>
        ///     <para>
        ///         If you do not set a value for <see cref="LocalCertificatePath" />, the default local user directory will be
        ///         used for your OS.
        ///     </para>
        ///     <para>Default: <see langword="false" /></para>
        /// </summary>
        public bool ForceCleanupOnShutdown { get; }

        /// <summary>
        /// Used to carry the PFX through the generation and installation lifetime. Not passed as an option.
        /// </summary>
        internal X509Certificate2? PFX { get; set; }

        /// <summary>
        /// Used to carry the CRT through the generation and installation lifetime. Not passed as an option.
        /// </summary>
        internal string? CRT { get; set; }
    }
}

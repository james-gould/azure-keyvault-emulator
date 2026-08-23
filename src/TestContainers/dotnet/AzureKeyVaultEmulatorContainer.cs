using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AzureKeyVaultEmulator.TestContainers.Constants;
using DotNet.Testcontainers.Containers;

namespace AzureKeyVaultEmulator.TestContainers
{
    /// <inheritdoc cref="DockerContainer" />
    public sealed class AzureKeyVaultEmulatorContainer : DockerContainer
    {
        private readonly AzureKeyVaultEmulatorConfiguration _configuration;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureKeyVaultEmulatorContainer" /> class.
        /// </summary>
        /// <param name="configuration">The container configuration.</param>
        public AzureKeyVaultEmulatorContainer(AzureKeyVaultEmulatorConfiguration configuration) : base(configuration) =>
            _configuration = configuration;

        /// <summary>
        ///     Gets the Azure Key Vault Emulator connection string.
        /// </summary>
        /// <returns>The Azure Key Vault Emulator connection string.</returns>
        public string GetConnectionString() =>
            $"https://{Hostname}:{GetMappedPublicPort(AzureKeyVaultEmulatorBuilder.AzureKeyVaultEmulatorPort)}";

        protected override ValueTask DisposeAsyncCore()
        {
            if (_configuration.ForceCleanupOnShutdown)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    UninstallContainerCertificates();
                }
                else
                {
                    Debug.WriteLine(
                        $"To remove the container certificates you must remove {AzureKeyVaultEmulatorCertConstants.Crt} from your Trusted Root CA store in the User location.");
                    Debug.WriteLine(
                        @"Execute sudo rm /usr/local/share/ca-certificates/mycert.crt \n sudo update-ca-certificates --fresh");
                }
            }

            return base.DisposeAsyncCore();
        }

        private void UninstallContainerCertificates()
        {
            string? thumbprint = _configuration.PFX?.Thumbprint;

            if (string.IsNullOrEmpty(thumbprint))
            {
                return; // hmm
            }

            using X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            X509Certificate2Collection certsToRemove =
                store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);

            foreach (X509Certificate2 cert in certsToRemove)
            {
                store.Remove(cert);
            }

            store.Close();
        }
    }
}

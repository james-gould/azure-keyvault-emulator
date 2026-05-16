using System;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;

namespace AzureKeyVaultEmulator.Aspire.Client
{
    /// <summary>
    /// Helper class for creating Azure KeyVault clients configured for the emulator.
    /// </summary>
    public static class KeyVaultHelper
    {
        /// <summary>
        /// Gets a <see cref="SecretClient"/> configured for the Azure KeyVault Emulator.
        /// </summary>
        /// <param name="vaultEndpoint">The emulator endpoint URL.</param>
        /// <returns>A configured <see cref="SecretClient"/>.</returns>
        public static SecretClient GetSecretClient(string vaultEndpoint)
        {
            if (string.IsNullOrEmpty(vaultEndpoint))
                throw new ArgumentNullException(nameof(vaultEndpoint));

            return new SecretClient(new Uri(vaultEndpoint), CreateCredential(), new SecretClientOptions { DisableChallengeResourceVerification = true });
        }

        /// <summary>
        /// Gets a <see cref="KeyClient"/> configured for the Azure KeyVault Emulator.
        /// </summary>
        /// <param name="vaultEndpoint">The emulator endpoint URL.</param>
        /// <returns>A configured <see cref="KeyClient"/>.</returns>
        public static KeyClient GetKeyClient(string vaultEndpoint)
        {
            if (string.IsNullOrEmpty(vaultEndpoint))
                throw new ArgumentNullException(nameof(vaultEndpoint));

            return new KeyClient(new Uri(vaultEndpoint), CreateCredential(), new KeyClientOptions { DisableChallengeResourceVerification = true });
        }

        /// <summary>
        /// Gets a <see cref="CertificateClient"/> configured for the Azure KeyVault Emulator.
        /// </summary>
        /// <param name="vaultEndpoint">The emulator endpoint URL.</param>
        /// <returns>A configured <see cref="CertificateClient"/>.</returns>
        public static CertificateClient GetCertificateClient(string vaultEndpoint)
        {
            if (string.IsNullOrEmpty(vaultEndpoint))
                throw new ArgumentNullException(nameof(vaultEndpoint));

            return new CertificateClient(new Uri(vaultEndpoint), CreateCredential(), new CertificateClientOptions { DisableChallengeResourceVerification = true });
        }

        private static DefaultAzureCredential CreateCredential() =>
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                DisableInstanceDiscovery = true,
            });
    }
}

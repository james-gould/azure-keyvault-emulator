using System;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AzureKeyVaultEmulator.Aspire.Client
{
    public static class AddEmulatorSupport
    {
        /// <summary>
        /// Creates the scaffolding for AzureKeyVault support using the containerised emulator.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to inject into.</param>
        /// <param name="vaultEndpoint">The endpoint from the for the containerised AzureKeyVaultEmulator. <br />Typically found in <see cref="IHostApplicationBuilder.Configuration"/></param>
        /// <param name="secrets">Bool to create a <see cref="SecretClient"/>, defaults to <see langword="true"/></param>
        /// <param name="keys">Bool to create a <see cref="KeyClient"/>, defaults to <see langword="false"/></param>
        /// <param name="certificates">Bool to create a <see cref="CertificateClient"/>, defaults to <see langword="false"/></param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if you attempt to use the Emulator outside of a DEBUG environment.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if you do not provide the BaseUrl for the KeyVaultEmulator container</exception>
        /// <returns>An updated <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddAzureKeyVaultEmulator(
            this IServiceCollection services,
            string vaultEndpoint,
            bool secrets = true,
            bool keys = false,
            bool certificates = false)
        {
            if (string.IsNullOrEmpty(vaultEndpoint))
                throw new ArgumentNullException(vaultEndpoint);

            var credential = new EmulatedTokenCredential(vaultEndpoint);
            var uri = new Uri(vaultEndpoint);

            if (secrets)
                services.AddTransient(x =>
                    new SecretClient(uri, credential, new SecretClientOptions { DisableChallengeResourceVerification = true }));

            if (keys)
                services.AddTransient(x =>
                    new KeyClient(uri, credential, new KeyClientOptions { DisableChallengeResourceVerification = true }));

            if (certificates)
                services.AddTransient(x =>
                    new CertificateClient(uri, credential, new CertificateClientOptions { DisableChallengeResourceVerification = true }));

            return services;
        }

        /// <summary>
        /// Gets a <see cref="SecretClient"/> configured for the Azure KeyVault Emulator.
        /// </summary>
        /// <param name="vaultEndpoint">The emulator endpoint URL.</param>
        /// <returns>A configured <see cref="SecretClient"/>.</returns>
        public static SecretClient GetSecretClient(string vaultEndpoint)
        {
            if (string.IsNullOrEmpty(vaultEndpoint))
                throw new ArgumentNullException(nameof(vaultEndpoint));

            var credential = new EmulatedTokenCredential(vaultEndpoint);
            var uri = new Uri(vaultEndpoint);

            return new SecretClient(uri, credential, new SecretClientOptions { DisableChallengeResourceVerification = true });
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

            var credential = new EmulatedTokenCredential(vaultEndpoint);
            var uri = new Uri(vaultEndpoint);

            return new KeyClient(uri, credential, new KeyClientOptions { DisableChallengeResourceVerification = true });
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

            var credential = new EmulatedTokenCredential(vaultEndpoint);
            var uri = new Uri(vaultEndpoint);

            return new CertificateClient(uri, credential, new CertificateClientOptions { DisableChallengeResourceVerification = true });
        }
    }
}

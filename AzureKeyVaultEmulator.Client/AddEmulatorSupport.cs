using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace AzureKeyVaultEmulator.Aspire.Client
{
    public static class AddEmulatorSupport
    {
        /// <summary>
        /// Creates the scaffolding for AzureKeyVault support using the containerised emulator.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to inject into.</param>
        /// <param name="vaultEndpoint">The endpoint from the for the containerised AzureKeyVaultEmulator. <br />Typically found in <see cref="IHostApplicationBuilder.Configuration.GetConnectionString(applicationName)"/></param>
        /// <param name="secrets">Bool to create a <see cref="SecretClient"/>, defaults to <see cref="true"/></param>
        /// <param name="keys">Bool to create a <see cref="KeyClient"/>, defaults to <see cref="false"/></param>
        /// <param name="certificates">Bool to create a <see cref="CertificateClient"/>, defaults to <see cref="false"/></param>
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
            if(string.IsNullOrEmpty(vaultEndpoint))
                throw new ArgumentNullException(vaultEndpoint);

            var credential = new EmulatedTokenCredential(vaultEndpoint);
            var uri = new Uri(vaultEndpoint);

            if (secrets)
                services.AddTransient(x =>
                    new SecretClient(uri, credential, new SecretClientOptions { DisableChallengeResourceVerification = true }));

            if (keys)
                services.AddTransient(x =>
                    new KeyClient(uri, credential, new KeyClientOptions { DisableChallengeResourceVerification = true }));

            if(certificates)
                services.AddTransient(x => 
                    new CertificateClient(uri, credential, new CertificateClientOptions { DisableChallengeResourceVerification = true }));

            return services;
        }
    }
}

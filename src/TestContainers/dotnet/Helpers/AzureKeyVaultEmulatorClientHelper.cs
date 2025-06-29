using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.TestContainers.Models;

namespace AzureKeyVaultEmulator.TestContainers.Helpers;

public static class AzureKeyVaultEmulatorClientHelper
{
    /// <summary>
    /// Gets a <see cref="SecretClient"/> configured for the Azure KeyVault Emulator.
    /// </summary>
    /// <param name="container">The TestContainers container hosting the AzureKeyVaultEmulator image.</param>
    /// <returns>A configured <see cref="SecretClient"/>.</returns>
    public static SecretClient GetSecretClient(this AzureKeyVaultEmulatorContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        var vaultEndpoint = container.GetConnectionString();

        if (string.IsNullOrEmpty(vaultEndpoint))
            throw new ArgumentNullException(nameof(vaultEndpoint));

        var credential = new EmulatedTokenCredential(vaultEndpoint);
        var uri = new Uri(vaultEndpoint);

        return new SecretClient(uri, credential, new SecretClientOptions { DisableChallengeResourceVerification = true });
    }

    /// <summary>
    /// Gets a <see cref="KeyClient"/> configured for the Azure KeyVault Emulator.
    /// </summary>
    /// <param name="container">The TestContainers container hosting the AzureKeyVaultEmulator image.</param>
    /// <returns>A configured <see cref="KeyClient"/>.</returns>
    public static KeyClient GetKeyClient(this AzureKeyVaultEmulatorContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        var vaultEndpoint = container.GetConnectionString();

        if (string.IsNullOrEmpty(vaultEndpoint))
            throw new ArgumentNullException(nameof(vaultEndpoint));

        var credential = new EmulatedTokenCredential(vaultEndpoint);
        var uri = new Uri(vaultEndpoint);

        return new KeyClient(uri, credential, new KeyClientOptions { DisableChallengeResourceVerification = true });
    }

    /// <summary>
    /// Gets a <see cref="CertificateClient"/> configured for the Azure KeyVault Emulator.
    /// </summary>
    /// /// <param name="container">The TestContainers container hosting the AzureKeyVaultEmulator image.</param>
    /// <returns>A configured <see cref="CertificateClient"/>.</returns>
    public static CertificateClient GetCertificateClient(this AzureKeyVaultEmulatorContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        var vaultEndpoint = container.GetConnectionString();

        if (string.IsNullOrEmpty(vaultEndpoint))
            throw new ArgumentNullException(nameof(vaultEndpoint));

        var credential = new EmulatedTokenCredential(vaultEndpoint);
        var uri = new Uri(vaultEndpoint);

        return new CertificateClient(uri, credential, new CertificateClientOptions { DisableChallengeResourceVerification = true });
    }
}

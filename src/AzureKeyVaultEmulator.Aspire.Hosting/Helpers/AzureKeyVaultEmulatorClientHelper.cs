using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;

namespace AzureKeyVaultEmulator.Aspire.Hosting.Helpers;

internal static class AzureKeyVaultEmulatorClientHelper
{
    internal static SecretClient GetSecretClient(Uri uri)
    {
        var opt = new SecretClientOptions { DisableChallengeResourceVerification = true };

        var credential = GetCredential();

        return new SecretClient(uri, credential, opt);
    }

    internal static CertificateClient GetCertificateClient(Uri uri)
    {
        var opt = new CertificateClientOptions { DisableChallengeResourceVerification = true };

        var credential = GetCredential();

        return new CertificateClient(uri, credential, opt);
    }

    internal static KeyClient GetKeyClient(Uri uri)
    {
        var opt = new KeyClientOptions { DisableChallengeResourceVerification = true };

        var credential = GetCredential();

        return new KeyClient(uri, credential, opt);
    }

    internal static TokenCredential GetCredential()
    {
        var opt = new DefaultAzureCredentialOptions
        {
            DisableInstanceDiscovery = true
        };

        return new DefaultAzureCredential(opt);
    }
}

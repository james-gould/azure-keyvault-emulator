using Azure.Core;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;

namespace AzureKeyVaultEmulator.Aspire.Hosting.Helpers;

internal static class AzureKeyVaultEmulatorClientHelper
{
    internal const string _httpClientName = "AzureKeyVaultEmulator.Aspire.Hosting";

    internal static SecretClient GetSecretClient(string vaultUri, IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        var opt = new SecretClientOptions { DisableChallengeResourceVerification = true };

        var uri = new Uri(vaultUri);

        var credential = new EmulatedTokenCredential(uri, httpClientFactory);

        return new SecretClient(uri, credential, opt);
    }

    internal static CertificateClient GetCertificateClient(string vaultUri, IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        var opt = new CertificateClientOptions { DisableChallengeResourceVerification = true };

        var uri = new Uri(vaultUri);

        var credential = new EmulatedTokenCredential(uri, httpClientFactory);

        return new CertificateClient(uri, credential, opt);
    }

    internal static KeyClient GetKeyClient(string vaultUri, IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        var opt = new KeyClientOptions { DisableChallengeResourceVerification = true };

        var uri = new Uri(vaultUri);

        var credential = new EmulatedTokenCredential(uri, httpClientFactory);

        return new KeyClient(uri, credential, opt);
    }

    private class EmulatedTokenCredential(Uri vaultUri, IHttpClientFactory httpClientFactory) : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => GetTokenAsync(requestContext, cancellationToken).AsTask().GetAwaiter().GetResult();

        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var client = httpClientFactory.CreateClient(_httpClientName);
            using (var response = await client.GetAsync(new Uri(vaultUri, "token"), cancellationToken))
            {
                var content = await response.Content.ReadAsStringAsync();

                return new AccessToken(content, DateTimeOffset.Now.AddYears(1));
            }
        }
    }
}

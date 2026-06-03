using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using System.Net;

namespace AzureKeyVaultEmulator.Aspire.Hosting.Helpers;

internal static class AzureKeyVaultEmulatorClientHelper
{
    internal static SecretClient GetSecretClient(string vaultUri)
    {
        var opt = new SecretClientOptions
        {
            DisableChallengeResourceVerification = true,
            Transport = new HttpClientTransport(CreateHttpClient())
        };

        var uri = new Uri(vaultUri);

        var credential = new EmulatedTokenCredential(uri);

        return new SecretClient(uri, credential, opt);
    }

    internal static CertificateClient GetCertificateClient(string vaultUri)
    {
        var opt = new CertificateClientOptions
        {
            DisableChallengeResourceVerification = true,
            Transport = new HttpClientTransport(CreateHttpClient())
        };

        var uri = new Uri(vaultUri);

        var credential = new EmulatedTokenCredential(uri);

        return new CertificateClient(uri, credential, opt);
    }

    internal static KeyClient GetKeyClient(string vaultUri)
    {
        var opt = new KeyClientOptions
        {
            DisableChallengeResourceVerification = true,
            Transport = new HttpClientTransport(CreateHttpClient())
        };

        var uri = new Uri(vaultUri);

        var credential = new EmulatedTokenCredential(uri);

        return new KeyClient(uri, credential, opt);
    }

    private class EmulatedTokenCredential(Uri vaultUri) : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => GetTokenAsync(requestContext, cancellationToken).AsTask().GetAwaiter().GetResult();

        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            using var client = CreateHttpClient();
            var response = await client.GetAsync(new Uri(vaultUri, "token"), cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return new AccessToken(content, DateTimeOffset.Now.AddYears(1));
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (request, _, _, sslErrors) =>
            {
                if (request?.RequestUri is null)
                    return false;

                return IsLoopback(request.RequestUri) || sslErrors == System.Net.Security.SslPolicyErrors.None;
            }
        };

        return new HttpClient(handler, disposeHandler: true);
    }

    private static bool IsLoopback(Uri uri)
    {
        if (uri.IsLoopback)
            return true;

        return IPAddress.TryParse(uri.Host, out var address) && IPAddress.IsLoopback(address);
    }
}

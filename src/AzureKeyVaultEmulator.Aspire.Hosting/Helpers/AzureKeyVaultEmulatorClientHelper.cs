using Azure.Core;
using Azure.Security.KeyVault.Secrets;

namespace AzureKeyVaultEmulator.Aspire.Hosting.Helpers;

internal static class AzureKeyVaultEmulatorClientHelper
{
    internal static SecretClient GetSecretClient(string vaultUri)
    {
        var opt = new SecretClientOptions { DisableChallengeResourceVerification = true };

        var uri = new Uri(vaultUri);

        var credential = new EmulatedTokenCredential(uri);

        return new SecretClient(uri, credential, opt);
    }

    private class EmulatedTokenCredential(Uri vaultUri) : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => GetTokenAsync(requestContext, cancellationToken).AsTask().GetAwaiter().GetResult();

        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            using var client = new HttpClient();

            try
            {
                client.BaseAddress = vaultUri;

                var response = await client.GetAsync("token");

                var content = await response.Content.ReadAsStringAsync();

                return new AccessToken(content, DateTimeOffset.Now.AddYears(1));
            }
            catch
            {
                throw;
            }
            finally
            {
                client?.Dispose();
            }
        }
    }
}

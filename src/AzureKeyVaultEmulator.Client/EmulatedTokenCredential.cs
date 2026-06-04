using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace AzureKeyVaultEmulator.Aspire.Client
{
    /// <summary>
    /// Legacy <see cref="TokenCredential"/> that fetches a bearer token from the emulator's
    /// <c>/token</c> endpoint. The emulator now self-hosts an Entra-compatible OAuth2 authority,
    /// so <see cref="Azure.Identity.DefaultAzureCredential"/> can be used directly instead.
    /// </summary>
    [Obsolete("EmulatedTokenCredential is no longer required. The emulator now exposes an Entra-compatible OAuth2 surface, so use Azure.Identity.DefaultAzureCredential (with DefaultAzureCredentialOptions { DisableInstanceDiscovery = true }) instead. This type will be removed in a future major version.")]
    public sealed class EmulatedTokenCredential : TokenCredential
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public EmulatedTokenCredential(string vaultUri)
        {
            _emulatedVaultUri = vaultUri;
        }

        private string _emulatedVaultUri = string.Empty;
        private string _token = string.Empty;
        private DateTimeOffset _expiry => DateTimeOffset.Now.AddDays(1);

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            // Hate this but someone somewhere will be using Sync methods...
            var token = GetBearerToken(cancellationToken).GetAwaiter().GetResult();

            return new AccessToken(token, _expiry);
        }

        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var token = await GetBearerToken(cancellationToken);

            return new AccessToken(token, _expiry);
        }

        private async ValueTask<string> GetBearerToken(CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(_token))
                return _token;

            if (string.IsNullOrEmpty(_emulatedVaultUri))
                throw new ArgumentNullException(nameof(_emulatedVaultUri));

            using (var response = await _httpClient.GetAsync(
                new Uri(new Uri(_emulatedVaultUri), "token"),
                cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                return _token = await response.Content.ReadAsStringAsync();
            }
        }
    }
}

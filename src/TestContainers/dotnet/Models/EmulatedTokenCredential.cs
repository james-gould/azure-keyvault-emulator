using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace AzureKeyVaultEmulator.TestContainers.Models
{
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

        /// <summary>
        /// Worth revisiting this as a typed client or similar, the wiring is a nightmare.
        /// Alternatively we could attempt to patch this in using Aspire events, requires research.
        /// </summary>
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

using Azure.Core;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Aspire.Client
{
    public sealed class EmulatedTokenCredential : TokenCredential
    {
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
            var token = GetBearerToken().GetAwaiter().GetResult();

            return new AccessToken(token, _expiry);
        }

        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var token = await GetBearerToken();

            return new AccessToken(token, _expiry);
        }

        /// <summary>
        /// Worth revisiting this as a typed client or similar, the wiring is a nightmare.
        /// Alternatively we could attempt to patch this in using Aspire events, requires research.
        /// </summary>
        private async ValueTask<string> GetBearerToken()
        {
            if (!string.IsNullOrEmpty(_token))
                return _token;

            if(string.IsNullOrEmpty(_emulatedVaultUri))
                throw new ArgumentNullException(nameof(_emulatedVaultUri));

            HttpClient client = null;

            try
            {
                client = new HttpClient();

                var response = await client.GetAsync($"{_emulatedVaultUri}/token");

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                throw;
            }
            finally
            {
                client.Dispose();
            }
        }
    }
}

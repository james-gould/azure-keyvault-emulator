using Azure.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper
{
    /// <summary>
    /// Used to create a bearer token with the emulated /token endpoint, which then passes the auth checks internally
    /// </summary>
    /// <param name="token"></param>
    internal sealed class EmulatedTokenCredential(string token) : TokenCredential
    {
        private string _token = token;

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(_token, DateTimeOffset.Now.AddDays(30));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
        }
    }
}

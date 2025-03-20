using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Shared.Utilities
{
    public static class HttpRequestUtils
    {
        /// <summary>
        /// Provides the nextLink property when retrieving an IEnumerable<T> from a vault. <br/>
        /// Used when a MaxCount optional parameter is provided.
        /// </summary>
        public static string GetNextLink(this HttpContext context, string skipToken, int max = 25, string origin = "")
        {
            var builder = new Uri($"{context.Request.Scheme}://{context.Request.Path}/");

            var baseUrl = builder.AbsoluteUri;

            var queryParam = $"?skipToken={skipToken}&maxresults={max}";

            if (!string.IsNullOrEmpty(origin))
                baseUrl += $"/origin";

            return $"{baseUrl}{queryParam}";
        }
    }
}

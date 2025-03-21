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
        private const string apiVersion = "api-version";
        /// <summary>
        /// Provides the nextLink property when retrieving an IEnumerable<T> from a vault. <br/>
        /// Used when a MaxCount optional parameter is provided.
        /// </summary>
        public static string GetNextLink(this IHttpContextAccessor context, string skipToken, int max = 25)
        {
            ArgumentNullException.ThrowIfNull(context.HttpContext);

            var http = context.HttpContext;

            var exists = http.Request.Query.TryGetValue(apiVersion, out var version);

            if (!exists)
                throw new InvalidOperationException($"Could not parse api-version header when generated nextLink");

            var builder = new Uri($"{http.Request.Scheme}://{http.Request.Host}{http.Request.Path}");

            var queryParam = $"?{apiVersion}={version}&$skipToken={skipToken}&maxresults={max}";

            return $"{builder.AbsoluteUri}{queryParam}";
        }
    }
}

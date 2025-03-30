using Microsoft.AspNetCore.Http;

namespace AzureKeyVaultEmulator.Shared.Utilities
{
    public static class HttpRequestUtils
    {
        private const string _apiVersion = "api-version";
        /// <summary>
        /// Provides the nextLink property when retrieving an <see cref="IEnumerable{T}"/> from a vault. <br/>
        /// Used when a MaxCount optional parameter is provided.
        /// </summary>
        public static string GetNextLink(this IHttpContextAccessor context, string skipToken, int max = 25)
        {
            ArgumentNullException.ThrowIfNull(context.HttpContext);

            var http = context.HttpContext;

            var exists = http.Request.Query.TryGetValue(_apiVersion, out var version);

            if (!exists)
                throw new InvalidOperationException($"Could not parse api-version header when generated nextLink");

            var builder = new Uri($"{http.Request.Scheme}://{http.Request.Host}{http.Request.Path}");

            var queryParam = $"?{_apiVersion}={version}&$skipToken={skipToken}&maxresults={max}";

            return $"{builder.AbsoluteUri}{queryParam}";
        }
    }
}

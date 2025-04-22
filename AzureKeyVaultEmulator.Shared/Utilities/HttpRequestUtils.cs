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

        /// <summary>
        /// Constructs the URI that's used to create KeyIdentifier, SecretIdentifier or CertificateIdentifier.
        /// </summary>
        /// <param name="context">The <see cref="IHttpContextAccessor"/> from the request.</param>
        /// <param name="name">The name of the item to create an identifier for.</param>
        /// <param name="path">The path of the request, typically the name of the item being created (keys/secrets/certificates).</param>
        /// <param name="version">Optional version.</param>
        /// <returns>A fully compliant <see cref="Uri"/></returns>
        public static string BuildIdentifierUri(this IHttpContextAccessor context, string name, string version, string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var builder = new UriBuilder
            {
                Scheme = context.HttpContext?.Request.Scheme,
                Host = context.HttpContext?.Request.Host.Host,
                Port = context.HttpContext?.Request.Host.Port ?? -1,
                Path = $"{path}/{name}/{version}"
            };

            return builder.Uri.ToString();
        }
    }
}

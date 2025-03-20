using System;
using System.Collections.Concurrent;
using AzureKeyVaultEmulator.Shared.Exceptions;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using Microsoft.AspNetCore.Http;

namespace AzureKeyVaultEmulator.Secrets.Services
{
    public class KeyVaultSecretService : IKeyVaultSecretService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly ConcurrentDictionary<string, SecretResponse?> _secrets = new();
        private static readonly ConcurrentDictionary<string, SecretResponse?> _deletedSecrets = new();

        public KeyVaultSecretService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public SecretResponse? Get(string name, string version = "")
        {
            _secrets.TryGetValue(GetSecretCacheId(name, version), out var found);

            return found;
        }

        public SecretResponse? SetSecret(string name, SetSecretModel secret)
        {
            var version = Guid.NewGuid().ToString();
            var secretUrl = new UriBuilder
            {
                Scheme = _httpContextAccessor.HttpContext?.Request.Scheme,
                Host = _httpContextAccessor.HttpContext?.Request.Host.Host,
                Port = _httpContextAccessor.HttpContext?.Request.Host.Port ?? -1,
                Path = $"secrets/{name}/{version}"
            };

            var response = new SecretResponse
            {
                Id = secretUrl.Uri,
                Value = secret.Value,
                Attributes = secret.SecretAttributes,
                Tags = secret.Tags
            };

            _secrets.AddOrUpdate(GetSecretCacheId(name), response, (_, _) => response);
            _secrets.TryAdd(GetSecretCacheId(name, version), response);

            return response;
        }

        public DeletedSecretBundle? DeleteSecret(string name, string version = "")
        {
            var cacheId = GetSecretCacheId(name, version);

            var removed = _secrets.TryRemove(cacheId, out var secret);

            if (!removed || secret is null)
                throw new SecretException($"Failed to remove secret with name: {name}");

            var deleted = new DeletedSecretBundle
            {
                Attributes = secret.Attributes,
                SecretId = secret.Id?.ToString() ?? string.Empty,
                Tags = secret.Tags,
                Value = secret.Value
            };

            _deletedSecrets.TryAdd(name, secret);

            return deleted;
        }

        private static string GetSecretCacheId(string name, string version = "") => $"{name}{version}";
    }
}

using System;
using System.Collections.Concurrent;
using AzureKeyVaultEmulator.Secrets.Models;
using Microsoft.AspNetCore.Http;

namespace AzureKeyVaultEmulator.Secrets.Services
{
    public interface IKeyVaultSecretService
    {
        SecretResponse? Get(string name);
        SecretResponse? Get(string name, string version);
        SecretResponse? SetSecret(string name, SetSecretModel requestBody);
    }

    public class KeyVaultSecretService : IKeyVaultSecretService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly ConcurrentDictionary<string, SecretResponse> Secrets = new();

        public KeyVaultSecretService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public SecretResponse? Get(string name)
        {
            Secrets.TryGetValue(GetSecretCacheId(name), out var found);

            return found;
        }

        public SecretResponse? Get(string name, string version)
        {
            Secrets.TryGetValue(GetSecretCacheId(name, version), out var found);

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

            Secrets.AddOrUpdate(GetSecretCacheId(name), response, (_, _) => response);
            Secrets.TryAdd(GetSecretCacheId(name, version), response);

            return response;
        }

        private static string GetSecretCacheId(string name, string version = "") => $"{name}{version}";
    }
}

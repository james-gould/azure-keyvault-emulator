using System;
using System.Collections.Concurrent;
using AzureKeyVaultEmulator.Keys.Factories;
using AzureKeyVaultEmulator.Keys.Models;
using AzureKeyVaultEmulator.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace AzureKeyVaultEmulator.Keys.Services
{
    public interface IKeyVaultKeyService
    {
        KeyResponse? Get(string name);
        KeyResponse? Get(string name, string version);
        KeyResponse? CreateKey(string name, CreateKeyModel key);

        KeyOperationResult? Encrypt(string name, string version, KeyOperationParameters keyOperationParameters);
        KeyOperationResult? Decrypt(string keyName, string keyVersion, KeyOperationParameters keyOperationParameters);
    }

    public class KeyVaultKeyService : IKeyVaultKeyService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly ConcurrentDictionary<string, KeyResponse> Keys = new();

        public KeyVaultKeyService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public KeyResponse? Get(string name)
        {
            Keys.TryGetValue(GetCacheId(name), out var found);

            return found;
        }

        public KeyResponse? Get(string name, string version)
        {
            Keys.TryGetValue(GetCacheId(name, version), out var found);

            return found;
        }

        public KeyResponse? CreateKey(string name, CreateKeyModel key)
        {
            var JWKS = GetJWKSFromModel(key);

            var version = Guid.NewGuid().ToString();
            var keyUrl = new UriBuilder
            {
                Scheme = _httpContextAccessor.HttpContext?.Request.Scheme,
                Host = _httpContextAccessor.HttpContext?.Request.Host.Host,
                Port = _httpContextAccessor.HttpContext?.Request.Host.Port ?? -1,
                Path = $"keys/{name}/{version}"
            };

            JWKS.KeyName = name;
            JWKS.KeyVersion = version;
            JWKS.KeyIdentifier = keyUrl.Uri.ToString();
            JWKS.KeyOperations = key.KeyOperations;

            var response = new KeyResponse
            {
                Key = JWKS,
                Attributes = key.KeyAttributes,
                Tags = key.Tags
            };

            Keys.AddOrUpdate(GetCacheId(name), response, (_, _) => response);
            Keys.TryAdd(GetCacheId(name, version), response);

            return response;
        }

        public KeyOperationResult? Encrypt(string name, string version, KeyOperationParameters keyOperationParameters)
        {
            if (!Keys.TryGetValue(GetCacheId(name, version), out var foundKey))
                throw new Exception("Key not found");

            var encrypted = Base64UrlEncoder.Encode(foundKey.Key.Encrypt(keyOperationParameters));

            return new KeyOperationResult
            {
                KeyIdentifier = foundKey.Key.KeyIdentifier,
                Data = encrypted
            };
        }

        public KeyOperationResult? Decrypt(string keyName, string keyVersion, KeyOperationParameters keyOperationParameters)
        {
            if (!Keys.TryGetValue(GetCacheId(keyName, keyVersion), out var foundKey))
                throw new Exception("Key not found");

            var decrypted = foundKey.Key.Decrypt(keyOperationParameters);

            return new KeyOperationResult
            {
                KeyIdentifier = foundKey.Key.KeyIdentifier,
                Data = decrypted
            };
        }

        private JsonWebKeyModel GetJWKSFromModel(CreateKeyModel key)
        {
            switch (key.KeyType)
            {
                case RSAKeyTypes.RSA:
                    var rsaKey = RsaKeyFactory.CreateRsaKey(key.KeySize);
                    return new JsonWebKeyModel(rsaKey);

                case RSAKeyTypes.EC:
                    throw new NotImplementedException("Elliptic Curve keys are not currently supported.");

                default:
                    throw new NotImplementedException($"KeyType {key.KeyType} is not supported");
            }
        }

        private static string GetCacheId(string name, string version = "") => $"{name}{version}";
    }
}

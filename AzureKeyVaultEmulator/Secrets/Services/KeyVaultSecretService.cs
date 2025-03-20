using System;
using System.Collections.Concurrent;
using System.Text;
using AzureKeyVaultEmulator.Emulator.Services;
using AzureKeyVaultEmulator.Shared.Exceptions;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using AzureKeyVaultEmulator.Shared.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace AzureKeyVaultEmulator.Secrets.Services
{
    public class KeyVaultSecretService : IKeyVaultSecretService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenService _token;

        private static readonly ConcurrentDictionary<string, SecretResponse?> _secrets = new();
        private static readonly ConcurrentDictionary<string, SecretResponse?> _deletedSecrets = new();

        public KeyVaultSecretService(IHttpContextAccessor httpContextAccessor, ITokenService token)
        {
            _httpContextAccessor = httpContextAccessor;
            _token = token;
        }

        public SecretResponse? Get(string name, string version = "")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var cacheId = name.GetCacheId(version);

            var exists = _secrets.TryGetValue(cacheId, out var secret);

            if (!exists || secret is null)
                throw new SecretException($"Failed to find secret by name: {name}");

            return secret;
        }

        public SecretResponse? SetSecret(string name, SetSecretModel secret)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(secret);

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

            _secrets.AddOrUpdate(name.GetCacheId(), response, (_, _) => response);
            _secrets.TryAdd(name.GetCacheId(version), response);

            return response;
        }

        public DeletedSecretBundle? DeleteSecret(string name, string version = "")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var cacheId = name.GetCacheId(version);

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

            _deletedSecrets.TryAdd(cacheId, secret);

            return deleted;
        }

        public BackupSecretResult? BackupSecret(string name)
        {
            var cacheId = name.GetCacheId();

            var exists = _secrets.TryGetValue(cacheId, out var secret);

            if (!exists || secret is null)
                throw new SecretException($"Cannot backup secret by name {name} because it does not exist");

            return new BackupSecretResult
            {
                Value = secret.Id.Base64UrlEncode()
            };
        }

        public SecretResponse? GetDeletedSecret(string name)
        {
            var cacheId = name.GetCacheId();

            var exists = _secrets.TryGetValue(cacheId, out var secret);

            if (!exists || secret is null)
                throw new SecretException($"Cannot get deleted secret with name: {name} because it does not exist");

            return secret;
        }

        public ListResult<SecretResponse> GetDeletedSecrets(int maxResults = 25, int skipCount = 0)
        {
            if (maxResults is default(int) && skipCount is default(int))
                return new();

            if(maxResults > _deletedSecrets.Count)
                maxResults = _deletedSecrets.Count;

            var items = _deletedSecrets.Skip(skipCount).Take(maxResults);

            if (!items.Any())
                return new();

            var secrets = items.Select(kvp => kvp.Value);

            var skipToken = _token.CreateSkipToken(maxResults);

            return new ListResult<SecretResponse>
            {
                NextLink = _httpContextAccessor.HttpContext?.GetNextLink(skipToken, maxResults) ?? string.Empty,
                Values = secrets
            };
        }

        public ListResult<SecretResponse> GetSecretVersions(string secretName, int maxResults = 25, int skipCount = 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var items = _secrets.Where(x => x.Key.Contains(secretName)).Skip(skipCount).Take(maxResults);

            if (!items.Any())
                return new();

            if (items.Count() < maxResults)
                maxResults = items.Count();

            var secrets = items.Select(x => x.Value);

            var skipToken = _token.CreateSkipToken(maxResults);

            return new ListResult<SecretResponse>
            {
                NextLink = _httpContextAccessor.HttpContext?.GetNextLink(skipToken, maxResults) ?? string.Empty,
                Values = secrets
            };
        }

        public ListResult<SecretResponse> GetSecrets(int maxResults = 25, int skipCount = 0)
        {
            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var items = _secrets.Skip(skipCount).Take(maxResults);

            if(!items.Any()) 
                return new();

            if(items.Count() < maxResults)
                maxResults = items.Count();

            var secrets = items.Select(x => x.Value);

            var skipToken = _token.CreateSkipToken(maxResults);

            return new ListResult<SecretResponse>
            {
                NextLink = _httpContextAccessor.HttpContext?.GetNextLink(skipToken, maxResults) ?? string.Empty,
                Values = secrets
            };
        }

        public void PurgeDeletedSecret(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var exists = _deletedSecrets.TryGetValue(name, out _);

            if (!exists)
                throw new SecretException($"Not deleted secret with the name: {name} was found");

            _deletedSecrets.Remove(name, out _);
        }

        public SecretResponse? RecoverDeletedSecret(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var exists = _deletedSecrets.TryGetValue(name, out var secret);

            if (!exists)
                throw new SecretException($"Cannot recover secret with name: {name}, secret not found");

            var added = _secrets.TryAdd(name, secret);

            if (!added)
                throw new SecretException($"Failed to recover the secret from deleted storage, no action taken");

            _deletedSecrets.Remove(name, out _);

            return secret;
        }

        public SecretResponse? RestoreSecret(string encodedSecretId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(encodedSecretId);

            var decoded = EncodingUtils.Base64UrlDecode(encodedSecretId);

            if (string.IsNullOrEmpty(decoded))
                return new();

            var uri = new Uri(decoded);

            var secretById = _secrets.FirstOrDefault(x => x.Value?.Id == uri).Value;

            if (secretById is null)
                throw new SecretException($"Failed to restore secret from backup blob");

            return secretById;
        }

        public void UpdateSecret(string name, string version, SecretAttributesModel? attributes = null, string contentType = "", object? tags = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var cacheId = name.GetCacheId(version);

            var exists = _secrets.TryGetValue(cacheId, out var secret);

            if (!exists || secret is null)
                throw new SecretException($"Cannot find secret with name {name} and version {version}");

            if (attributes is not null)
                secret.Attributes = attributes;

            if(!string.IsNullOrEmpty(contentType))
                secret.ContentType = contentType;

            if(tags is not null)
                secret.Tags = tags;

            secret.Attributes.Update();

            _secrets.TryUpdate(cacheId, secret, null);
        }
    }
}

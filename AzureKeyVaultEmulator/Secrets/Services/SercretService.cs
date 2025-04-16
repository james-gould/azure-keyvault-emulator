using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Secrets.Services
{
    public class SercretService(
        IHttpContextAccessor httpContextAccessor,
        ITokenService token,
        IEncryptionService encryption) : ISecretService
    {
        private static readonly ConcurrentDictionary<string, SecretResponse?> _secrets = new();
        private static readonly ConcurrentDictionary<string, SecretResponse?> _deletedSecrets = new();

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
                Scheme = httpContextAccessor.HttpContext?.Request.Scheme,
                Host = httpContextAccessor.HttpContext?.Request.Host.Host,
                Port = httpContextAccessor.HttpContext?.Request.Host.Port ?? -1,
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

        public ValueModel<string>? BackupSecret(string name)
        {
            var cacheId = name.GetCacheId();

            var exists = _secrets.TryGetValue(cacheId, out var secret);

            if (!exists || secret is null)
                throw new SecretException($"Cannot backup secret by name {name} because it does not exist");

            return new ValueModel<string>
            {
                Value = encryption.CreateKeyVaultJwe(secret)
            };
        }

        public SecretResponse? GetDeletedSecret(string name)
        {
            var cacheId = name.GetCacheId();

            var exists = _deletedSecrets.TryGetValue(cacheId, out var secret);

            if (!exists || secret is null)
                throw new SecretException($"Cannot get deleted secret with name: {name} because it does not exist");

            return secret;
        }

        public ListResult<SecretResponse> GetDeletedSecrets(int maxResults = 25, int skipCount = 0)
        {
            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var items = _deletedSecrets.Skip(skipCount).Take(maxResults);

            if (!items.Any())
                return new();

            var requiresPaging = items.Count() >= maxResults;

            return new ListResult<SecretResponse>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = items.Select(kvp => kvp.Value)
            };
        }

        public ListResult<SecretResponse> GetSecretVersions(string secretName, int maxResults = 25, int skipCount = 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var allItems = _secrets.Where(x => x.Key.Contains(secretName));

            if (!allItems.Any())
                return new();

            var maxedItems = allItems.Skip(skipCount).Take(maxResults);

            var requiresPaging = maxedItems.Count() >= maxResults;

            return new ListResult<SecretResponse>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = maxedItems.Select(x => x.Value)
            };
        }

        public ListResult<SecretResponse> GetSecrets(int maxResults = 25, int skipCount = 0)
        {
            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var items = _secrets.Skip(skipCount).Take(maxResults);

            if (!items.Any())
                return new();

            var requiresPaging = items.Count() >= maxResults;

            return new ListResult<SecretResponse>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = items.Select(x => x.Value)
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

            return encryption.DecryptFromKeyVaultJwe<SecretResponse?>(encodedSecretId);
        }

        public SecretAttributesModel UpdateSecret(string name, string version, SecretAttributesModel attributes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var cacheId = name.GetCacheId(version);

            var exists = _secrets.TryGetValue(cacheId, out var secret);

            if (!exists || secret is null)
                throw new SecretException($"Cannot find secret with name {name} and version {version}");

            if (!string.IsNullOrEmpty(attributes.ContentType))
                secret.Attributes.ContentType = attributes.ContentType;

            secret.Attributes.Update();

            _secrets.TryUpdate(cacheId, secret, null);

            return secret.Attributes;
        }

        private string GenerateNextLink(int maxResults)
        {
            var skipToken = token.CreateSkipToken(maxResults);

            return httpContextAccessor.GetNextLink(skipToken, maxResults);
        }
    }
}

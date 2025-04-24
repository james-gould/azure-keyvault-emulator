using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Secrets.Services
{
    public class SecretService(
        IHttpContextAccessor httpContextAccessor,
        ITokenService token,
        IEncryptionService encryption) : ISecretService
    {
        private static readonly ConcurrentDictionary<string, SecretBundle> _secrets = new();
        private static readonly ConcurrentDictionary<string, SecretBundle?> _deletedSecrets = new();

        public SecretBundle GetSecret(string name, string version = "")
        {
            return _secrets.SafeGet(name.GetCacheId(version));
        }

        public SecretBundle SetSecret(string name, SetSecretModel secret)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(secret);

            var version = Guid.NewGuid().ToString();

            var secretUri = httpContextAccessor.BuildIdentifierUri(name, version, "secrets");

            var response = new SecretBundle
            {
                SecretIdentifier = secretUri,
                Value = secret.Value,
                Attributes = secret.SecretAttributes,
                Tags = secret.Tags
            };

            _secrets.SafeAddOrUpdate(name.GetCacheId(), response);
            _secrets.SafeAddOrUpdate(name.GetCacheId(version), response);

            return response;
        }

        public DeletedSecretBundle DeleteSecret(string name, string version = "")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var cacheId = name.GetCacheId(version);

            var removed = _secrets.TryRemove(cacheId, out var secret);

            if (!removed || secret is null)
                throw new SecretException($"Failed to remove secret with name: {name}");

            var deleted = new DeletedSecretBundle
            {
                Name = name,
                RecoveryId = secret.SecretIdentifier,
                Attributes = secret.Attributes,
                SecretId = secret.SecretIdentifier,
                Tags = secret.Tags,
                Value = secret.Value
            };

            _deletedSecrets.TryAdd(cacheId, secret);

            return deleted;
        }

        public ValueModel<string> BackupSecret(string name)
        {
            var cacheId = name.GetCacheId();

            var secret = _secrets.SafeGet(cacheId);

            return new ValueModel<string>
            {
                Value = encryption.CreateKeyVaultJwe(secret)
            };
        }

        public SecretBundle? GetDeletedSecret(string name)
        {
            var cacheId = name.GetCacheId();
            
            var secret = _secrets.SafeGet(cacheId);

            return secret;
        }

        public ListResult<SecretBundle> GetDeletedSecrets(int maxResults = 25, int skipCount = 0)
        {
            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var items = _deletedSecrets.Skip(skipCount).Take(maxResults);

            if (!items.Any())
                return new();

            var requiresPaging = items.Count() >= maxResults;

            return new ListResult<SecretBundle>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = items.Select(kvp => kvp.Value)
            };
        }

        public ListResult<SecretBundle> GetSecretVersions(string secretName, int maxResults = 25, int skipCount = 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var allItems = _secrets.Where(x => x.Key.Contains(secretName));

            if (!allItems.Any())
                return new();

            var maxedItems = allItems.Skip(skipCount).Take(maxResults);

            var requiresPaging = maxedItems.Count() >= maxResults;

            return new ListResult<SecretBundle>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = maxedItems.Select(x => x.Value)
            };
        }

        public ListResult<SecretBundle> GetSecrets(int maxResults = 25, int skipCount = 0)
        {
            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var items = _secrets.Skip(skipCount).Take(maxResults);

            if (!items.Any())
                return new();

            var requiresPaging = items.Count() >= maxResults;

            return new ListResult<SecretBundle>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = items.Select(x => x.Value)
            };
        }

        public void PurgeDeletedSecret(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var secret = _secrets.SafeGet(name);
            
            _deletedSecrets.Remove(name, out _);
        }

        public SecretBundle? RecoverDeletedSecret(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            
            var secret = _secrets.SafeGet(name);

            var added = _secrets.TryAdd(name, secret);

            if (!added)
                throw new SecretException($"Failed to recover the secret from deleted storage, no action taken");

            _deletedSecrets.Remove(name, out _);

            return secret!;
        }

        public SecretBundle? RestoreSecret(string encodedSecretId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(encodedSecretId);

            return encryption.DecryptFromKeyVaultJwe<SecretBundle?>(encodedSecretId);
        }

        public SecretAttributesModel UpdateSecret(string name, string version, SecretAttributesModel attributes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var cacheId = name.GetCacheId(version);

            var secret = _secrets.SafeGet(cacheId);

            if (!string.IsNullOrEmpty(attributes.ContentType))
                secret.Attributes.ContentType = attributes.ContentType;

            secret.Attributes.Update();

            _secrets.TryUpdate(cacheId, secret, null!);

            return secret.Attributes;
        }

        private string GenerateNextLink(int maxResults)
        {
            var skipToken = token.CreateSkipToken(maxResults);

            return httpContextAccessor.GetNextLink(skipToken, maxResults);
        }
    }
}

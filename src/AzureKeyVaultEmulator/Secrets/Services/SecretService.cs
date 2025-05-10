using AzureKeyVaultEmulator.Shared.Models.Secrets;
using AzureKeyVaultEmulator.Shared.Models.Secrets.Requests;
using AzureKeyVaultEmulator.Shared.Persistence;

namespace AzureKeyVaultEmulator.Secrets.Services
{
    public class SecretService(
        IHttpContextAccessor httpContextAccessor,
        ITokenService token,
        IEncryptionService encryption,
        VaultContext context) : ISecretService
    {
        private static readonly ConcurrentDictionary<string, SecretBundle> _deletedSecrets = new();

        public async Task<SecretBundle> GetSecretAsync(string name, string version = "")
        {
            return await context.Secrets.SafeGetAsync(name.GetCacheId(version));
        }

        public async Task<SecretBundle> SetSecretAsync(string name, SetSecretRequest secret)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(secret);

            var version = Guid.NewGuid().Neat();

            var secretUri = httpContextAccessor.BuildIdentifierUri(name, version, "secrets");

            var response = new SecretBundle
            {
                SecretIdentifier = secretUri,
                Value = secret.Value,
                Attributes = secret.SecretAttributes,
                Tags = secret.Tags
            };

            await context.Secrets.SafeAddOrUpdateAsync(name, "", response, context);
            await context.Secrets.SafeAddOrUpdateAsync(name, version, response.Clone(), context);

            await context.SaveChangesAsync();

            return response;
        }

        public async Task<SecretAttributesModel> UpdateSecretAsync(string name, string version, SecretAttributesModel attributes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var secret = await context.Secrets.SafeGetAsync(name, version);

            if (!string.IsNullOrEmpty(attributes.ContentType))
                secret.Attributes.ContentType = attributes.ContentType;

            secret.Attributes.Update();

            await context.Secrets.SafeAddOrUpdateAsync(name, version, secret, context);

            await context.SaveChangesAsync();

            return secret.Attributes;
        }

        public async Task<DeletedSecretBundle> DeleteSecretAsync(string name, string version = "")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var cacheId = name.GetCacheId(version);

            var secret = await context.Secrets.SafeGetAsync(cacheId);

            var deleted = new DeletedSecretBundle
            {
                RecoveryId = secret.SecretIdentifier,
                Attributes = secret.Attributes,
                SecretId = secret.SecretIdentifier,
                Tags = secret.Tags,
                Value = secret.Value
            };

            secret.Deleted = true;

            await context.Secrets.SafeAddOrUpdateAsync(cacheId, secret, context);

            await context.SaveChangesAsync();

            return deleted;
        }

        public async Task<ValueModel<string>> BackupSecretAsync(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            var cacheId = name.GetCacheId();

            var secret = await context.Secrets.SafeGetAsync(cacheId);

            return new ValueModel<string>
            {
                Value = encryption.CreateKeyVaultJwe(secret)
            };
        }

        public SecretBundle? GetDeletedSecret(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            var cacheId = name.GetCacheId();
            
            var secret = _deletedSecrets.SafeGet(cacheId);

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

            var allItems = context.Secrets.Where(x => x.PersistedName.Contains(secretName)).ToList();

            if (allItems.Count == 0)
                return new();

            var maxedItems = allItems.Skip(skipCount).Take(maxResults);

            var requiresPaging = maxedItems.Count() >= maxResults;

            return new ListResult<SecretBundle>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = maxedItems
            };
        }

        public ListResult<SecretBundle> GetSecrets(int maxResults = 25, int skipCount = 0)
        {
            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var items = context.Secrets.Skip(skipCount).Take(maxResults);

            if (!items.Any())
                return new();

            var requiresPaging = items.Count() >= maxResults;

            return new ListResult<SecretBundle>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = items
            };
        }

        public void PurgeDeletedSecret(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var secret = _deletedSecrets.SafeGet(name);
            
            _deletedSecrets.SafeRemove(name);
        }

        public async Task<SecretBundle> RecoverDeletedSecretAsync(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            
            var secret = _deletedSecrets.SafeGet(name);

            await context.Secrets.SafeAddOrUpdateAsync(name, secret, context);

            await context.SaveChangesAsync();

            _deletedSecrets.SafeRemove(name);

            return secret;
        }

        public async Task<SecretBundle> RestoreSecretAsync(string encodedSecretId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(encodedSecretId);

            var secret = encryption.DecryptFromKeyVaultJwe<SecretBundle>(encodedSecretId);

            var version = Guid.NewGuid().Neat();

            await context.Secrets.SafeAddOrUpdateAsync(secret.PersistedName.GetCacheId(version), secret, context);
            await context.SaveChangesAsync();

            return secret;
        }

        private string GenerateNextLink(int maxResults)
        {
            var skipToken = token.CreateSkipToken(maxResults);

            return httpContextAccessor.GetNextLink(skipToken, maxResults);
        }
    }
}

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
        public async Task<SecretBundle> GetSecretAsync(string name, string version = "")
        {
            return await context.Secrets.SafeGetAsync(name, version);
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

            await context.Secrets.SafeAddAsync(name, version, response);

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

            await context.SaveChangesAsync();

            return secret.Attributes;
        }

        public async Task<DeletedSecretBundle> DeleteSecretAsync(string name, string version = "")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var secret = await context.Secrets.SafeGetAsync(name, version);

            var deleted = new DeletedSecretBundle
            {
                RecoveryId = secret.SecretIdentifier,
                Attributes = secret.Attributes,
                SecretId = secret.SecretIdentifier,
                Tags = secret.Tags,
                Value = secret.Value
            };

            secret.Deleted = true;

            await context.SaveChangesAsync();

            return deleted;
        }

        public async Task<ValueModel<string>> BackupSecretAsync(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            var secret = await context.Secrets.SafeGetAsync(name);

            return new ValueModel<string>
            {
                Value = encryption.CreateKeyVaultJwe(secret)
            };
        }

        public async Task<SecretBundle> GetDeletedSecretAsync(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            
            var secret = await context.Secrets.SafeGetAsync(name, deleted: true);

            return secret;
        }

        public ListResult<SecretBundle> GetDeletedSecrets(int maxResults = 25, int skipCount = 0)
        {
            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var items = context.Secrets.Where(x => x.Deleted == true).Skip(skipCount).Take(maxResults);

            if (!items.Any())
                return new();

            var requiresPaging = items.Count() >= maxResults;

            return new ListResult<SecretBundle>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = items
            };
        }

        public ListResult<SecretBundle> GetSecretVersions(string secretName, int maxResults = 25, int skipCount = 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var allItems = context.Secrets.Where(x => x.PersistedName == secretName).ToList();

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

        public async Task PurgeDeletedSecretAsync(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            await context.Secrets.SafeRemoveAsync(name, deleted: true);

            await context.SaveChangesAsync();
        }

        public async Task<SecretBundle> RecoverDeletedSecretAsync(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            
            var secret = await context.Secrets.SafeGetAsync(name, deleted: true);

            secret.Deleted = false;

            await context.SaveChangesAsync();

            return secret;
        }

        public async Task<SecretBundle> RestoreSecretAsync(string encodedSecretId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(encodedSecretId);

            var secret = encryption.DecryptFromKeyVaultJwe<SecretBundle>(encodedSecretId);

            var version = Guid.NewGuid().Neat();

            await context.Secrets.SafeAddAsync(secret.PersistedName, version, secret);
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

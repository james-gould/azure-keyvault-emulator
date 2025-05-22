using AzureKeyVaultEmulator.Shared.Models.Secrets;
using AzureKeyVaultEmulator.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AzureKeyVaultEmulator.Keys.Services
{
    public class KeyService(
        IHttpContextAccessor httpContextAccessor,
        IEncryptionService encryptionService,
        ITokenService tokenService,
        VaultContext context)
        : IKeyService
    {
        private static readonly ConcurrentDictionary<string, KeyRotationPolicy> _keyRotations = new();

        public async Task<KeyBundle> GetKeyAsync(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            return await context.Keys.SafeGetAsync(name);
        }

        public async Task<KeyBundle> GetKeyAsync(string name, string version)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            return await context.Keys.SafeGetAsync(name, version);
        }

        public async Task<KeyBundle> CreateKeyAsync(string name, CreateKeyModel key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var JWKS = GetJWKSFromModel(key.KeySize, key.KeyType);

            var version = Guid.NewGuid().ToString();
            var keyUrl = httpContextAccessor.BuildIdentifierUri(name, version, "keys");

            JWKS.KeyName = name;
            JWKS.KeyVersion = version;
            JWKS.KeyIdentifier = keyUrl;
            JWKS.KeyOperations = key.KeyOperations;

            var response = new KeyBundle
            {
                Key = JWKS,
                Attributes = key.KeyAttributes,
                Tags = key.Tags ?? []
            };

            await context.Keys.SafeAddAsync(name, version, response);

            await context.SaveChangesAsync();

            return response;
        }

        public async Task<KeyAttributesModel?> UpdateKeyAsync(
            string name,
            string version,
            KeyAttributesModel attributes,
            Dictionary<string, string> tags)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var key = await context.Keys.SafeGetAsync(name, version);

            key.Attributes = attributes;
            key.Attributes.RecoverableDays = attributes.RecoverableDays;

            foreach (var tag in tags)
                key.Tags.TryAdd(tag.Key, tag.Value);

            key.Attributes.Update();

            await context.SaveChangesAsync();

            return key.Attributes;
        }

        public async Task<KeyBundle?> RotateKey(string name, string version)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var key = await context.Keys.SafeGetAsync(name, version);

            var newKey = new KeyBundle
            {
                Attributes = key.Attributes,
                Key = GetJWKSFromModel(key.Key.GetKeySize(), key.Key.KeyType),
                Tags = key.Tags
            };

            await context.Keys.SafeAddAsync(name, version, newKey);

            await context.SaveChangesAsync();

            return newKey;
        }

        public async Task<KeyOperationResult?> EncryptAsync(string name, string version, KeyOperationParameters keyOperationParameters)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var foundKey = await context.Keys.SafeGetAsync(name, version);

            var encrypted = EncodingUtils.Base64UrlEncode(foundKey.Key.Encrypt(keyOperationParameters));

            return new KeyOperationResult
            {
                KeyIdentifier = foundKey.Key.KeyIdentifier,
                Data = encrypted
            };
        }

        public async Task<KeyOperationResult?> DecryptAsync(string name, string version, KeyOperationParameters keyOperationParameters)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var foundKey = await context.Keys.SafeGetAsync(name, version);

            var decrypted = foundKey.Key.Decrypt(keyOperationParameters);

            return new KeyOperationResult
            {
                KeyIdentifier = foundKey.Key.KeyIdentifier,
                Data = decrypted
            };
        }

        public async Task<ValueModel<string>?> BackupKeyAsync(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var foundKey = await context.Keys.SafeGetAsync(name);

            return new ValueModel<string>
            {
                Value = encryptionService.CreateKeyVaultJwe(foundKey)
            };
        }

        public KeyBundle RestoreKey(string jweBody)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jweBody);

            return encryptionService.DecryptFromKeyVaultJwe<KeyBundle>(jweBody);
        }

        public ValueModel<string> GetRandomBytes(int count)
        {
            if (count > 128)
                throw new ArgumentException($"{nameof(count)} cannot exceed 128 when generating random bytes.");

            var bytes = new byte[count];

            Random.Shared.NextBytes(bytes);

            return new ValueModel<string>
            {
                Value = EncodingUtils.Base64UrlEncode(bytes)
            };
        }

        public KeyRotationPolicy GetKeyRotationPolicy(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            return _keyRotations.SafeGet(name.GetCacheId());
        }

        public async Task<KeyRotationPolicy> UpdateKeyRotationPolicyAsync(
            string name,
            KeyRotationAttributes attributes,
            IEnumerable<LifetimeActions> lifetimeActions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var key = await context.Keys.SafeGetAsync(name);

            var policyExists = _keyRotations.TryGetValue(name, out var keyRotationPolicy);

            if (!policyExists || keyRotationPolicy is null)
                keyRotationPolicy = new();

            keyRotationPolicy.Attributes = attributes;
            keyRotationPolicy.LifetimeActions = lifetimeActions;

            keyRotationPolicy.Attributes.Update();
            keyRotationPolicy.SetIdFromKeyName(name);

            _keyRotations.AddOrUpdate(name, keyRotationPolicy, (_, _) => keyRotationPolicy);

            return keyRotationPolicy;
        }

        public ListResult<KeyItemBundle> GetKeys(int maxResults = 25, int skipCount = 25)
        {
            if (maxResults is default(int) && skipCount is default(int))
                return new ListResult<KeyItemBundle>();

            var items = context.Keys.Skip(skipCount).Take(maxResults);

            if (!items.Any())
                return new ListResult<KeyItemBundle>();

            var requiresPaging = items.Count() >= maxResults;

            return new ListResult<KeyItemBundle>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = items.Select(ToKeyItemBundle)
            };
        }

        public ListResult<KeyItemBundle> GetKeyVersions(string name, int maxResults = 25, int skipCount = 25)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var allItems = context.Keys.Where(x => x.PersistedName == name);

            if (!allItems.Any())
                return new();

            var maxedItems = allItems.Skip(skipCount).Take(maxResults);

            var requiresPaging = maxedItems.Count() >= maxResults;

            return new ListResult<KeyItemBundle>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = maxedItems.Select(ToKeyItemBundle)
            };
        }

        public async Task<ValueModel<string>> ReleaseKeyAsync(string name, string version)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var key = await context.Keys.SafeGetAsync(name, version);

            var aasJwt = tokenService.CreateTokenWithHeaderClaim([], "keys", JsonSerializer.Serialize(key));

            var release = new KeyReleaseVM(aasJwt);

            return new ValueModel<string>
            {
                Value = encryptionService.CreateKeyVaultJwe(release)
            };
        }

        public async Task<KeyBundle> ImportKeyAsync(
            string name,
            JsonWebKey key,
            KeyAttributesModel attributes,
            Dictionary<string, string> tags)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var version = Guid.NewGuid().ToString();

            var jsonWebKey = new JsonWebKeyModel(key, name, version, httpContextAccessor.HttpContext);

            var response = new KeyBundle
            {
                Key = jsonWebKey,
                Attributes = attributes,
                Tags = tags
            };

            await context.Keys.SafeAddAsync(name, version, response);

            await context.SaveChangesAsync();

            return response;
        }

        public async Task<KeyOperationResult> SignWithKeyAsync(string name, string version, string algo, string digest)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(algo);
            ArgumentException.ThrowIfNullOrWhiteSpace(digest);

            var key = await context.Keys.SafeGetAsync(name, version);

            var signature = encryptionService.SignWithKey(key.Key.RSAKey, digest);

            return new KeyOperationResult
            {
                KeyIdentifier = key.Key.KeyIdentifier,
                Data = signature
            };
        }

        public async Task<ValueModel<bool>> VerifyDigestAsync(string name, string version, string digest, string signature)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(digest);
            ArgumentException.ThrowIfNullOrWhiteSpace(signature);

            var key = await context.Keys.SafeGetAsync(name, version);

            return new ValueModel<bool>
            {
                Value = encryptionService.VerifyData(key.Key.RSAKey, digest, signature)
            };
        }

        public async Task<KeyOperationResult> WrapKeyAsync(string name, string version, KeyOperationParameters para)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var key = await context.Keys.SafeGetAsync(name, version);

            var encrypted = key.Key.Encrypt(para);

            return new KeyOperationResult
            {
                KeyIdentifier = key.Key.KeyIdentifier,
                Data = EncodingUtils.Base64UrlEncode(encrypted)
            };
        }

        public async Task<KeyOperationResult> UnwrapKeyAsync(string name, string version, KeyOperationParameters para)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var key = await context.Keys.SafeGetAsync(name, version);

            var decrypted = key.Key.Decrypt(para);

            return new KeyOperationResult
            {
                KeyIdentifier = key.Key.KeyIdentifier,
                Data = decrypted
            };
        }

        public async Task<DeletedKeyBundle> DeleteKeyAsync(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var parentCacheId = name.GetCacheId();

            var parentKey = await context.Keys.SafeGetAsync(parentCacheId);

            var keys = await context.Keys.Where(x => x.PersistedName == name).ToListAsync();

            if (keys.Count == 0)
                throw new MissingItemException(name);

            foreach (var item in keys)
                item.Deleted = true;

            parentKey.Deleted = true;

            await context.SaveChangesAsync();

            return new DeletedKeyBundle
            {
                Kid = parentKey.Key.KeyIdentifier,
                Attributes = parentKey.Attributes,
                RecoveryId = $"{AuthConstants.EmulatorUri}/deletedkeys/{name}",
                Tags = parentKey.Tags,
                Key = new JsonWebKey(JsonSerializer.Serialize(parentKey.Key)),
            };
        }

        public async Task<KeyBundle> GetDeletedKeyAsync(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            return await context.Keys.SafeGetAsync(name, deleted: true);
        }

        public ListResult<KeyBundle> GetDeletedKeys(int maxResults = 25, int skipCount = 25)
        {
            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var allItems = context.Keys.Where(x => x.Deleted == true).ToList();

            if (allItems.Count == 0)
                return new();

            var maxedItems = allItems.Skip(skipCount).Take(maxResults);

            var requiresPaging = maxedItems.Count() >= maxResults;

            return new ListResult<KeyBundle>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = maxedItems
            };
        }

        public async Task PurgeDeletedKey(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            await context.Keys.SafeRemoveAsync(name, deleted: true);

            await context.SaveChangesAsync();
        }

        public async Task<KeyBundle> RecoverDeletedKeyAsync(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var key = await context.Keys.SafeGetAsync(name, deleted: true);

            key.Deleted = false;

            await context.Keys.SafeAddAsync(name, key.PersistedVersion, key);

            await context.SaveChangesAsync();

            return key;
        }

        private KeyItemBundle ToKeyItemBundle(KeyBundle bundle)
        {
            return new KeyItemBundle
            {
                KeyAttributes = bundle.Attributes,
                KeyId = bundle.Key.KeyIdentifier,
                Managed = false,
                Tags = bundle.Tags
            };
        }

        private static JsonWebKeyModel GetJWKSFromModel(int keySize, string keyType)
        {
            return keyType.ToUpper() switch
            {
                SupportedKeyTypes.RSA => new JsonWebKeyModel(RsaKeyFactory.CreateRsaKey(keySize)),
                SupportedKeyTypes.EC => throw new NotImplementedException("Elliptic Curve keys are not currently supported."),
                _ => throw new NotImplementedException($"Key type {keyType} is not supported")
            };
        }

        private string GenerateNextLink(int maxResults)
        {
            var skipToken = tokenService.CreateSkipToken(maxResults);

            return httpContextAccessor.GetNextLink(skipToken, maxResults);
        }
    }
}

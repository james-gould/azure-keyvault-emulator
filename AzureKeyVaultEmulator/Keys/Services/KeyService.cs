using System.Xml.Linq;
using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Keys.Services
{
    public class KeyService(
        IHttpContextAccessor httpContextAccessor,
        IEncryptionService encryptionService,
        ITokenService tokenService)
        : IKeyService
    {
        private static readonly ConcurrentDictionary<string, KeyBundle> _keys = new();
        private static readonly ConcurrentDictionary<string, KeyRotationPolicy> _keyRotations = new();

        private static readonly ConcurrentDictionary<string, KeyBundle> _deletedKeys = new();

        public KeyBundle GetKey(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            return _keys.SafeGet(name);
        }

        public KeyBundle GetKey(string name, string version)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            return _keys.SafeGet(name.GetCacheId(version));
        }

        public KeyBundle CreateKey(string name, CreateKeyModel key)
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

            _keys.AddOrUpdate(name.GetCacheId(), response, (_, _) => response);
            _keys.TryAdd(name.GetCacheId(version), response);

            return response;
        }

        public KeyAttributesModel? UpdateKey(
            string name,
            string version,
            KeyAttributesModel attributes,
            Dictionary<string, string> tags)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var cacheId = name.GetCacheId(version);

            var key = _keys.SafeGet(cacheId);

            key.Attributes = attributes;
            key.Attributes.RecoverableDays = attributes.RecoverableDays;

            foreach(var tag in tags)
                key.Tags.TryAdd(tag.Key, tag.Value);

            key.Attributes.Update();

            _keys.TryUpdate(cacheId, key, key);

            return key.Attributes;
        }

        public KeyBundle? RotateKey(string name, string version)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var cacheId = name.GetCacheId(version);

            var key = _keys.SafeGet(cacheId);

            var newKey = new KeyBundle
            {
                Attributes = key.Attributes,
                Key = GetJWKSFromModel(key.Key.GetKeySize(), key.Key.KeyType),
                Tags = key.Tags
            };

            _keys.TryUpdate(cacheId, newKey, key);

            return newKey;
        }

        public KeyOperationResult? Encrypt(string name, string version, KeyOperationParameters keyOperationParameters)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var foundKey = _keys.SafeGet(name.GetCacheId());

            var encrypted = EncodingUtils.Base64UrlEncode(foundKey.Key.Encrypt(keyOperationParameters));

            return new KeyOperationResult
            {
                KeyIdentifier = foundKey.Key.KeyIdentifier,
                Data = encrypted
            };
        }

        public KeyOperationResult? Decrypt(string name, string version, KeyOperationParameters keyOperationParameters)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var foundKey = _keys.SafeGet(name.GetCacheId());

            var decrypted = foundKey.Key.Decrypt(keyOperationParameters);

            return new KeyOperationResult
            {
                KeyIdentifier = foundKey.Key.KeyIdentifier,
                Data = decrypted
            };
        }

        public ValueModel<string>? BackupKey(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var foundKey = _keys.SafeGet(name.GetCacheId());

            return new ValueModel<string>
            {
                Value = encryptionService.CreateKeyVaultJwe(foundKey)
            };
        }

        public KeyBundle? RestoreKey(string jweBody)
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

        public KeyRotationPolicy UpdateKeyRotationPolicy(
            string name,
            KeyRotationAttributes attributes,
            IEnumerable<LifetimeActions> lifetimeActions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var key = _keys.SafeGet(name.GetCacheId());

            // Policy exists against overall key, not the current (cached) version
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
                return new();

            var items = _keys.Skip(skipCount).Take(maxResults);

            if (!items.Any())
                return new();

            var requiresPaging = items.Count() >= maxResults;

            return new ListResult<KeyItemBundle>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = items.Select(x => ToKeyItemBundle(x.Value))
            };
        }

        public ListResult<KeyItemBundle> GetKeyVersions(string name, int maxResults = 25, int skipCount = 25)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var allItems = _keys.Where(x => x.Key.Contains(name));

            if (!allItems.Any())
                return new();

            var maxedItems = allItems.Skip(skipCount).Take(maxResults);

            var requiresPaging = maxedItems.Count() >= maxResults;

            return new ListResult<KeyItemBundle>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = maxedItems.Select(x => ToKeyItemBundle(x.Value))
            };
        }

        public ValueModel<string> ReleaseKey(string name,string version)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var cacheId = name.GetCacheId(version);

            var key = _keys.SafeGet(cacheId);

            var aasJwt = tokenService.CreateTokenWithHeaderClaim([], "keys", JsonSerializer.Serialize(key));

            var release = new KeyReleaseVM(aasJwt);

            return new ValueModel<string>
            {
                Value = encryptionService.CreateKeyVaultJwe(release)
            };
        }

        public KeyBundle ImportKey(
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

            _keys.AddOrUpdate(name.GetCacheId(), response, (_, _) => response);
            _keys.TryAdd(name.GetCacheId(version), response);

            return response;
        }

        public KeyOperationResult SignWithKey(string name, string version, string algo, string digest)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(algo);
            ArgumentException.ThrowIfNullOrWhiteSpace(digest);

            var cacheId = name.GetCacheId(version);

            var key = _keys.SafeGet(cacheId);

            var signature = encryptionService.SignWithKey(digest);

            return new KeyOperationResult
            {
                KeyIdentifier = key.Key.KeyIdentifier,
                Data = signature
            };
        }

        public ValueModel<bool> VerifyDigest(string name, string version, string digest, string signature)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(digest);
            ArgumentException.ThrowIfNullOrWhiteSpace(signature);

            // Key unused here because we are signing with global RSA.
            // Might be worth breaking this out in the future to allow for direct key signing
            // Would be better behaviour, but maybe too much for a mocking tool :)
            var key = _keys.SafeGet(name.GetCacheId(version));

            return new ValueModel<bool>
            {
                Value = encryptionService.VerifyData(digest, signature)
            };
        }

        public KeyOperationResult WrapKey(string name, string version, KeyOperationParameters para)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var cacheId = name.GetCacheId(version);

            var key = _keys.SafeGet(cacheId);

            var encrypted = key.Key.Encrypt(para);

            return new KeyOperationResult
            {
                KeyIdentifier = key.Key.KeyIdentifier,
                Data = EncodingUtils.Base64UrlEncode(encrypted)
            };
        }

        public KeyOperationResult UnwrapKey(string name, string version, KeyOperationParameters para)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var cacheId = name.GetCacheId(version);

            var key = _keys.SafeGet(cacheId);

            var decrypted = key.Key.Decrypt(para);

            return new KeyOperationResult
            {
                KeyIdentifier = key.Key.KeyIdentifier,
                Data = decrypted
            };
        }

        public DeletedKeyBundle DeleteKey(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var parentKey = _keys.SafeGet(name.GetCacheId());

            var keys = _keys.Where(x => x.Key.Contains(name));

            if (!keys.Any())
                throw new InvalidOperationException($"Cannot find any keys with name: {name}");

            foreach (var item in keys)
            {
                _keys.Remove(item.Key, out _);
                _deletedKeys.TryAdd(name.GetCacheId(item.Value.Key.KeyVersion), item.Value);
            }

            _deletedKeys.TryAdd(name.GetCacheId(), parentKey);
            
            return new DeletedKeyBundle
            {
                Name = name,
                Kid = parentKey.Key.KeyIdentifier,
                Attributes = parentKey.Attributes,
                RecoveryId = $"{AuthConstants.EmulatorUri}/deletedkeys/{name}",
                Tags = parentKey.Tags,
                Key = new JsonWebKey(JsonSerializer.Serialize(parentKey.Key)),
            };
        }

        public KeyBundle GetDeletedKey(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            return _deletedKeys.SafeGet(name);
        }

        public ListResult<KeyBundle> GetDeletedKeys(int maxResults = 25, int skipCount = 25)
        {
            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var allItems = _deletedKeys.ToList();

            if (allItems.Count == 0)
                return new();

            var maxedItems = allItems.Skip(skipCount).Take(maxResults);

            var requiresPaging = maxedItems.Count() >= maxResults;

            return new ListResult<KeyBundle>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = maxedItems.Select(x => x.Value)
            };
        }

        public void PurgeDeletedKey(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            // to ensure it exists in our deleted keys first
            _deletedKeys.SafeGet(name);

            _deletedKeys.Remove(name, out _);
        }

        public KeyBundle RecoverDeletedKey(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var toBeRestored = _deletedKeys.SafeGet(name);

            _deletedKeys.Remove(name, out _);

            _keys.TryAdd(name, toBeRestored);

            return toBeRestored;
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
            switch (keyType.ToUpper())
            {
                case SupportedKeyTypes.RSA:
                    var rsaKey = RsaKeyFactory.CreateRsaKey(keySize);
                    return new JsonWebKeyModel(rsaKey);

                case SupportedKeyTypes.EC:
                    throw new NotImplementedException("Elliptic Curve keys are not currently supported.");

                default:
                    throw new NotImplementedException($"Key type {keyType} is not supported");
            }
        }

        private string GenerateNextLink(int maxResults)
        {
            var skipToken = tokenService.CreateSkipToken(maxResults);

            return httpContextAccessor.GetNextLink(skipToken, maxResults);
        }
    }
}

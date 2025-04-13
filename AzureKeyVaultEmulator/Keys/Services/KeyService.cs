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
        private static readonly ConcurrentDictionary<string, string> _digests = new();

        private static readonly ConcurrentDictionary<string, KeyBundle> _deletedKeys = new();

        public KeyBundle? GetKey(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            return _keys.SafeGet(name);
        }

        public KeyBundle? GetKey(string name, string version)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            return _keys.SafeGet(name.GetCacheId(version));
        }

        public KeyBundle? CreateKey(string name, CreateKeyModel key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var JWKS = GetJWKSFromModel(key.KeySize, key.KeyType);

            var version = Guid.NewGuid().ToString();
            var keyUrl = new UriBuilder
            {
                Scheme = httpContextAccessor.HttpContext?.Request.Scheme,
                Host = httpContextAccessor.HttpContext?.Request.Host.Host,
                Port = httpContextAccessor.HttpContext?.Request.Host.Port ?? -1,
                Path = $"keys/{name}/{version}"
            };

            JWKS.KeyName = name;
            JWKS.KeyVersion = version;
            JWKS.KeyIdentifier = keyUrl.Uri.ToString();
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

        public KeyAttributesModel? UpdateKey(string name, string version, KeyAttributesModel attributes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var cacheId = name.GetCacheId(version);

            var key = _keys.SafeGet(cacheId);

            if (string.IsNullOrEmpty(attributes.ContentType))
                key.Attributes.ContentType = attributes.ContentType;

            key.Attributes.RecoverableDays = attributes.RecoverableDays;

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

            var encrypted = Base64UrlEncoder.Encode(foundKey.Key.Encrypt(keyOperationParameters));

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

        public ValueResponse? BackupKey(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var foundKey = _keys.SafeGet(name.GetCacheId());

            return new ValueResponse
            {
                Value = encryptionService.CreateKeyVaultJwe(foundKey)
            };
        }

        public KeyBundle? RestoreKey(string jweBody)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jweBody);

            return encryptionService.DecryptFromKeyVaultJwe<KeyBundle>(jweBody);
        }

        public ValueResponse GetRandomBytes(int count)
        {
            if (count > 128)
                throw new ArgumentException($"{nameof(count)} cannot exceed 128 when generating random bytes.");

            var bytes = new byte[count];

            Random.Shared.NextBytes(bytes);

            return new ValueResponse
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
                keyRotationPolicy = new(name);

            keyRotationPolicy.Attributes = attributes;

            keyRotationPolicy.Attributes.Update();

            keyRotationPolicy.LifetimeActions = lifetimeActions;

            _keyRotations.AddOrUpdate(name, keyRotationPolicy, (_, _) => keyRotationPolicy);

            return keyRotationPolicy;
        }

        public ListResult<KeyBundle> GetKeys(int maxResults = 25, int skipCount = 25)
        {
            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var items = _keys.Skip(skipCount).Take(maxResults);

            if (!items.Any())
                return new();

            var requiresPaging = items.Count() >= maxResults;

            return new ListResult<KeyBundle>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = items.Select(x => x.Value)
            };
        }

        public ListResult<KeyBundle> GetKeyVersions(string name, int maxResults = 25, int skipCount = 25)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (maxResults is default(int) && skipCount is default(int))
                return new();

            var allItems = _keys.Where(x => x.Key.Contains(name));

            if (!allItems.Any())
                return new();

            var maxedItems = allItems.Skip(skipCount).Take(maxResults);

            var requiresPaging = maxedItems.Count() >= maxResults;

            return new ListResult<KeyBundle>
            {
                NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
                Values = maxedItems.Select(x => x.Value)
            };
        }

        public ValueResponse ReleaseKey(string name,string version)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var cacheId = name.GetCacheId(version);

            var key = _keys.SafeGet(cacheId);

            var aasJwt = tokenService.CreateTokenWithHeaderClaim([], "keys", JsonSerializer.Serialize(key));

            var release = new KeyReleaseVM(aasJwt);

            return new ValueResponse
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

        public KeyOperationResult SignWithKey(string name, string version, string algo, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);
            ArgumentException.ThrowIfNullOrWhiteSpace(algo);
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            var cacheId = name.GetCacheId(version);

            var key = _keys.SafeGet(cacheId);

            var (hash, sig) = encryptionService.SignAndHashData(value);

            _digests.TryAdd(cacheId, hash);

            return new KeyOperationResult
            {
                KeyIdentifier = $"{AuthConstants.EmulatorUri}/keys/{name}/{key.Key.KeyIdentifier}",
                Data = sig
            };
        }

        public bool VerifyDigest(string name, string version, string digest, string signature)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);
            ArgumentException.ThrowIfNullOrWhiteSpace(digest);
            ArgumentException.ThrowIfNullOrWhiteSpace(signature);

            var cacheId = name.GetCacheId(version);

            var key = _keys.SafeGet(cacheId);
            var cachedDigest = _digests.SafeGet(cacheId);

            return encryptionService.VerifyData(cachedDigest, signature);
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

            var keys = _keys.Where(x => x.Key.Contains(name));

            if (!keys.Any())
                throw new InvalidOperationException($"Cannot find any keys with name: {name}");

            _ = keys.Select(x => _keys.Remove(x.Key, out _));

            keys.Select(x => _deletedKeys.TryAdd(x.Key, x.Value));

            var topLevel = keys.First().Value;

            return new DeletedKeyBundle
            {
                Attributes = topLevel.Attributes,
                RecoveryId = $"/deletedkeys/{name}/recover",
                Tags = topLevel.Tags,
                Key = new JsonWebKey(JsonSerializer.Serialize(topLevel.Key)),
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

            if (!allItems.Any())
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

        private static JsonWebKeyModel GetJWKSFromModel(int keySize, string keyType)
        {
            switch (keyType)
            {
                case RSAKeyTypes.RSA:
                    var rsaKey = RsaKeyFactory.CreateRsaKey(keySize);
                    return new JsonWebKeyModel(rsaKey);

                case RSAKeyTypes.EC:
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

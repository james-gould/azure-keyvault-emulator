namespace AzureKeyVaultEmulator.Keys.Services
{
    public class KeyService(
        IHttpContextAccessor httpContextAccessor,
        IJweEncryptionService jweEncryptionService)
        : IKeyService
    {
        private static readonly ConcurrentDictionary<string, KeyResponse> _keys = new();
        private static readonly ConcurrentDictionary<string, KeyRotationPolicy> _keyRotations = new();

        public KeyResponse? GetKey(string name)
        {
            return _keys.SafeGet(name);
        }

        public KeyResponse? GetKey(string name, string version)
        {
            return _keys.SafeGet(name.GetCacheId(version));
        }

        public KeyResponse? CreateKey(string name, CreateKeyModel key)
        {
            var JWKS = GetJWKSFromModel(key);

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

            var response = new KeyResponse
            {
                Key = JWKS,
                Attributes = key.KeyAttributes,
                Tags = key.Tags
            };

            _keys.AddOrUpdate(name.GetCacheId(), response, (_, _) => response);
            _keys.TryAdd(name.GetCacheId(version), response);

            return response;
        }

        public KeyOperationResult? Encrypt(string name, string version, KeyOperationParameters keyOperationParameters)
        {
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
            var foundKey = _keys.SafeGet(name.GetCacheId());

            return new ValueResponse
            {
                Value = jweEncryptionService.CreateKeyVaultJwe(foundKey)
            };
        }

        public KeyResponse? RestoreKey(string jweBody)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jweBody);

            return jweEncryptionService.DecryptFromKeyVaultJwe<KeyResponse>(jweBody);
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
            return _keyRotations.SafeGet(name.GetCacheId());
        }

        public KeyRotationPolicy UpdateKeyRotationPolicy(
            string name,
            KeyRotationAttributes attributes,
            IEnumerable<LifetimeActions> lifetimeActions)
        {
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
    }
}

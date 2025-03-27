using Microsoft.IdentityModel.Tokens;

namespace AzureKeyVaultEmulator.Keys.Services
{
    public class KeyService : IKeyService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly ConcurrentDictionary<string, KeyResponse> _keys = new();

        public KeyService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public KeyResponse? Get(string name)
        {
            _keys.TryGetValue(name.GetCacheId(), out var found);

            return found;
        }

        public KeyResponse? Get(string name, string version)
        {
            _keys.TryGetValue(name.GetCacheId(version), out var found);

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

            _keys.AddOrUpdate(name.GetCacheId(), response, (_, _) => response);
            _keys.TryAdd(name.GetCacheId(version), response);

            return response;
        }

        public KeyOperationResult? Encrypt(string name, string version, KeyOperationParameters keyOperationParameters)
        {
            if (!_keys.TryGetValue(name.GetCacheId(version), out var foundKey))
                throw new Exception("Key not found");

            var encrypted = Base64UrlEncoder.Encode(foundKey.Key.Encrypt(keyOperationParameters));

            return new KeyOperationResult
            {
                KeyIdentifier = foundKey.Key.KeyIdentifier,
                Data = encrypted
            };
        }

        public KeyOperationResult? Decrypt(string name, string version, KeyOperationParameters keyOperationParameters)
        {
            if (!_keys.TryGetValue(name.GetCacheId(version), out var foundKey))
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
    }
}

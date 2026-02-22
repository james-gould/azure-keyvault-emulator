using System.Security.Cryptography;

namespace AzureKeyVaultEmulator.Emulator.Services
{
    public interface IEncryptionService : IDisposable
    {
        string CreateKeyVaultJwe(object value);
        T DecryptFromKeyVaultJwe<T>(string jwe) where T : notnull;
        string SignWithKey(RSA key, string hashAlgo, string data);
        bool VerifyData(RSA key, string hashAlgo, string hash, string signature);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly RSA _rsa;

        public EncryptionService()
        {
            _rsa = RSA.Create();
            _rsa.ImportFromPem(RsaPem.FullPem);
        }

        public string SignWithKey(RSA key, string hashAlgo, string data)
        {
            var bytes = data.Base64UrlDecode();

            var signedBytes = key.SignHash(bytes, GetHashAlgo(hashAlgo), GetPadding(hashAlgo));

            return signedBytes.Base64UrlEncode();
        }

        public bool VerifyData(RSA key, string hashAlgo, string digest, string signature)
        {
            var hashBytes = digest.Base64UrlDecode();
            var sigBytes = signature.Base64UrlDecode();

            return key.VerifyHash(hashBytes, sigBytes, GetHashAlgo(hashAlgo), GetPadding(hashAlgo));
        }

        public T DecryptFromKeyVaultJwe<T>(string jweToken)
            where T : notnull
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jweToken);

            var decodedJwe = Encoding.UTF8.GetString(jweToken.Base64UrlDecode());

            var parts = decodedJwe.Split('.');

            var header = parts[0].Base64UrlDecode();
            var key = parts[1].Base64UrlDecode();
            var iv = parts[2].Base64UrlDecode();
            var payload = parts[3].Base64UrlDecode();

            var aesKey = _rsa.Decrypt(key, RSAEncryptionPadding.OaepSHA256);

            using var aes = Aes.Create();

            aes.Key = aesKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();

            var decryptedPayload = decryptor.TransformFinalBlock(payload, 0, payload.Length);

            var json = Encoding.UTF8.GetString(decryptedPayload);

            if (string.IsNullOrEmpty(json))
                throw new InvalidOperationException($"Failed to decrypt JSON string for {nameof(T)}");

            return JsonSerializer.Deserialize<T>(json)
                ?? throw new SecretException($"Failed to deserialize JSON to {nameof(T)}");
        }

        public string CreateKeyVaultJwe(object value)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(value);

            using var aes = Aes.Create();

            aes.GenerateKey();
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();

            var payloadBytes = encryptor.TransformFinalBlock(payload, 0, payload.Length);

            var header = new
            {
                alg = "RSA-OAEP",
                enc = "A256CBC-HS512"
            };

            var headerBytes = JsonSerializer.SerializeToUtf8Bytes(header);

            var keyBytes = _rsa.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);

            var jwe = $"{headerBytes.Base64UrlEncode()}.{keyBytes.Base64UrlEncode()}.{aes.IV.Base64UrlEncode()}.{payloadBytes.Base64UrlEncode()}";

            var bytes = Encoding.UTF8.GetBytes(jwe);

            return bytes.Base64UrlEncode();
        }

        public void Dispose()
        {
            _rsa.Dispose();
        }
        
        /// <summary>
        /// Converts a string name of a hashing algorithm to a <see cref="HashAlgorithmName"/>.
        /// </summary>
        /// <param name="algo">The signature algorithm name (e.g., RS256, PS384, ES512).</param>
        /// <returns>contains the corresponding hash algorithm if the conversion succeeded.</returns>
        /// <remarks>
        /// Based on https://github.com/Azure/azure-sdk-for-net/blob/c8964dc0d3101c9f34157c3db99e169e784ee08c/sdk/keyvault/Azure.Security.KeyVault.Keys/src/Cryptography/SignatureAlgorithm.cs#L209-L235
        /// </remarks>
        private HashAlgorithmName GetHashAlgo(string algo)
        {
            return algo switch
            {
                "RS256" or "PS256" => HashAlgorithmName.SHA256,
                "RS384" or "PS384" => HashAlgorithmName.SHA384,
                "RS512" or "PS512" => HashAlgorithmName.SHA512,
                _ => throw new ArgumentException($"Invalid signing algorithm: '{algo}'.")
            };
        }
        
        /// <summary>
        /// Get the padding for a <see cref="HashAlgorithmName"/> 
        /// </summary>
        /// <param name="algo"><see cref="HashAlgorithmName"/> to get the padding for</param>
        /// <returns>The <see cref="RSASignaturePadding"/> for the given <see cref="HashAlgorithmName"/></returns>
        /// <remarks>
        /// Based on https://github.com/Azure/azure-sdk-for-net/blob/c8964dc0d3101c9f34157c3db99e169e784ee08c/sdk/keyvault/Azure.Security.KeyVault.Keys/src/Cryptography/SignatureAlgorithm.cs#L258-L275
        /// </remarks>
        private RSASignaturePadding GetPadding(string algo)
        {
            return algo switch
            {
                "RS256" or "RS384" or "RS512" => RSASignaturePadding.Pkcs1,
                "PS256" or "PS384" or "PS512" => RSASignaturePadding.Pss,
                _ => throw new ArgumentException($"Invalid signing algorithm: '{algo}'.")
            };
        }
    }
}

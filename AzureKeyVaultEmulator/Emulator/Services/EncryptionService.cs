using System.Security.Cryptography;

namespace AzureKeyVaultEmulator.Emulator.Services
{
    public interface IEncryptionService : IDisposable
    {
        string CreateKeyVaultJwe(object value);
        T DecryptFromKeyVaultJwe<T>(string jwe);
        string SignWithKey(string data);
        bool VerifyData(string hash, string signature);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly RSA _rsa;

        private readonly RSASignaturePadding _padding = RSASignaturePadding.Pkcs1;
        private readonly HashAlgorithmName _hashingAlgorithm = HashAlgorithmName.SHA256;

        public EncryptionService()
        {
            _rsa = RSA.Create();
            _rsa.ImportFromPem(RsaPem.FullPem);
        }

        public string SignWithKey(string data)
        {
            var bytes = data.Base64UrlDecode();

            var signedBytes = _rsa.SignData(bytes, _hashingAlgorithm, _padding);

            return signedBytes.Base64UrlEncode();
        }

        public bool VerifyData(string digest, string signature)
        {
            var hashBytes = digest.Base64UrlDecode();
            var sigBytes = signature.Base64UrlDecode();

            return _rsa.VerifyHash(hashBytes, sigBytes, _hashingAlgorithm, _padding);
        }

        public T DecryptFromKeyVaultJwe<T>(string jweToken)
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

            return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException($"Failed to deserialize JSON to {nameof(T)}");
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

            return jwe.Base64UrlEncode();
        }

        public void Dispose()
        {
            _rsa.Dispose();
        }
    }
}

using System.Security.Cryptography;

namespace AzureKeyVaultEmulator.Emulator.Services
{
    public interface IJweEncryptionService : IDisposable
    {
        string CreateKeyVaultJwe(object value);
        T DecryptFromKeyVaultJwe<T>(string jwe);
    }

    public class JweEncryptionService : IJweEncryptionService
    {
        private readonly RSA _rsa;

        public JweEncryptionService()
        {
            _rsa = RSA.Create();
            _rsa.ImportFromPem(RsaPem.FullPem);
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

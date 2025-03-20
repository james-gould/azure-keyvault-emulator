using AzureKeyVaultEmulator.Shared.Constants;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys
{
    public class JsonWebKeyModel
    {
        [JsonPropertyName("crv")]
        public string KeyCurve { get; set; } = string.Empty;

        [JsonPropertyName("d")]
        [JsonIgnore]
        public string D { get; set; } = string.Empty;

        [JsonPropertyName("dp")]
        public string Dp { get; set; } = string.Empty;

        [JsonPropertyName("dq")]
        public string Dq { get; set; } = string.Empty;

        [JsonPropertyName("e")]
        public string E { get; set; } = string.Empty;

        [JsonPropertyName("k")]
        public string K { get; set; } = string.Empty;

        [JsonPropertyName("key_hsm")]
        public string KeyHsm { get; set; } = string.Empty;

        [JsonPropertyName("key_ops")]
        public List<string> KeyOperations { get; set; } = [];

        [JsonPropertyName("kty")]
        public string KeyType { get; set; } = string.Empty;

        [JsonPropertyName("kid")]
        public string KeyIdentifier { get; set; } = string.Empty;

        [JsonIgnore]
        public string KeyName { get; set; } = string.Empty;

        [JsonIgnore]
        public string KeyVersion { get; set; } = string.Empty;

        [JsonPropertyName("n")]
        public string N { get; set; } = string.Empty;

        [JsonPropertyName("p")]
        public string P { get; set; } = string.Empty;

        [JsonPropertyName("q")]
        public string Q { get; set; } = string.Empty;

        [JsonPropertyName("qi")]
        public string Qi { get; set; } = string.Empty;

        [JsonPropertyName("x")]
        public string X { get; set; } = string.Empty;

        [JsonPropertyName("y")]
        public string Y { get; set; } = string.Empty;

        private readonly RSA _rsaKey;
        private readonly RSAParameters _rsaParameters;

        public JsonWebKeyModel() : this(RSA.Create())
        {
        }

        public JsonWebKeyModel(RSA rsaKey)
        {
            _rsaKey = rsaKey;
            _rsaParameters = rsaKey.ExportParameters(true);
            D = Encoding.UTF8.GetString(_rsaParameters.D ?? []);
            Dp = Encoding.UTF8.GetString(_rsaParameters.DP ?? []);
            Dq = Encoding.UTF8.GetString(_rsaParameters.DQ ?? []);
            E = Encoding.UTF8.GetString(_rsaParameters.Exponent ?? []);
            D = Encoding.UTF8.GetString(_rsaParameters.D ?? []);
            KeyType = "RSA";
            N = Encoding.UTF8.GetString(_rsaParameters.Modulus ?? []);
            P = Encoding.UTF8.GetString(_rsaParameters.P ?? []);
            Q = Encoding.UTF8.GetString(_rsaParameters.Q ?? []);
            Qi = Encoding.UTF8.GetString(_rsaParameters.InverseQ ?? []);
        }

        public byte[] Encrypt(KeyOperationParameters data)
        {
            return data.Algorithm switch
            {
                EncryptionAlgorithms.RSA1_5 => RsaEncrypt(data.Data, RSAEncryptionPadding.Pkcs1),
                EncryptionAlgorithms.RSA_OAEP => RsaEncrypt(data.Data, RSAEncryptionPadding.OaepSHA1),
                _ => throw new NotImplementedException($"Algorithm '{data.Algorithm}' does not support Encryption")
            };
        }

        private byte[] RsaEncrypt(string plaintext, RSAEncryptionPadding padding)
        {
            using var rsaAlg = new RSACryptoServiceProvider(_rsaKey.KeySize);
            rsaAlg.ImportParameters(_rsaParameters);
            return rsaAlg.Encrypt(Encoding.UTF8.GetBytes(plaintext), padding);
        }

        public string Decrypt(KeyOperationParameters data)
        {
            return data.Algorithm switch
            {
                EncryptionAlgorithms.RSA1_5 => RsaDecrypt(data.Data, RSAEncryptionPadding.Pkcs1),
                EncryptionAlgorithms.RSA_OAEP => RsaDecrypt(data.Data, RSAEncryptionPadding.OaepSHA1),
                _ => throw new NotImplementedException($"Algorithm '{data.Algorithm}' does not support Decryption")
            };
        }

        private string RsaDecrypt(string ciphertext, RSAEncryptionPadding padding)
        {
            using var rsaAlg = new RSACryptoServiceProvider(_rsaKey.KeySize);
            rsaAlg.ImportParameters(_rsaParameters);
            return Encoding.UTF8.GetString(rsaAlg.Decrypt(Encoding.UTF8.GetBytes(ciphertext), padding));
        }
    }
}

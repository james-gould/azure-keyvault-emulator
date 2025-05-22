using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Constants;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;
using AzureKeyVaultEmulator.Shared.Persistence.Utils;
using AzureKeyVaultEmulator.Shared.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace AzureKeyVaultEmulator.Shared.Models.Keys
{
    public class JsonWebKeyModel : IPersistedItem
    {
        [Key]
        [JsonIgnore]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid PersistedId { get; set; } = Guid.NewGuid();

        [JsonPropertyName("crv")]
        public string? KeyCurve { get; set; } = string.Empty;

        [JsonPropertyName("d")]
        public string? D { get; set; } = string.Empty;

        [JsonPropertyName("dp")]
        public string? Dp { get; set; } = string.Empty;

        [JsonPropertyName("dq")]
        public string? Dq { get; set; } = string.Empty;

        [JsonPropertyName("e")]
        public string? E { get; set; } = string.Empty;

        [JsonPropertyName("k")]
        public string? K { get; set; } = string.Empty;

        [JsonPropertyName("key_hsm")]
        public string? KeyHsm { get; set; } = string.Empty;

        [JsonPropertyName("key_ops")]
        public List<string>? KeyOperations { get; set; }

        [JsonPropertyName("kty")]
        public string KeyType { get; set; } = string.Empty;

        [JsonPropertyName("kid")]
        public string KeyIdentifier { get; set; } = string.Empty;

        [JsonIgnore]
        public string? KeyName { get; set; } = string.Empty;

        [JsonIgnore]
        public string KeyVersion { get; set; } = string.Empty;

        [JsonPropertyName("n")]
        public string? N { get; set; } = string.Empty;

        [JsonPropertyName("p")]
        public string? P { get; set; } = string.Empty;

        [JsonPropertyName("q")]
        public string? Q { get; set; } = string.Empty;

        [JsonPropertyName("qi")]
        public string? Qi { get; set; } = string.Empty;

        [JsonPropertyName("x")]
        public string? X { get; set; } = string.Empty;

        [JsonPropertyName("y")]
        public string? Y { get; set; } = string.Empty;

        [JsonIgnore]
        public byte[] RSAParametersBlob { get; set; } = [];

        private RSA? _backingRsaKey;

        [NotMapped]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public RSA RSAKey
        {
            get
            {
                if(_backingRsaKey != null)
                    return _backingRsaKey;

                _backingRsaKey = RSA.Create();
                _backingRsaKey.ImportParameters(RsaParametersSerializer.Deserialize(RSAParametersBlob));

                return _backingRsaKey;
            }
            set => RSAParametersBlob = RsaParametersSerializer.Serialize(value.ExportParameters(true));
        }

        [NotMapped]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public RSAParameters RSAParameters => RsaParametersSerializer.Deserialize(RSAParametersBlob);

        public JsonWebKeyModel() : this(RSA.Create()) { }

        public JsonWebKeyModel(RSA rsaKey)
        {
            RSAKey = rsaKey;

            KeyType = SupportedKeyTypes.RSA;

            D = EncodingUtils.Base64UrlEncode(RSAParameters.D ?? []);
            Dp = EncodingUtils.Base64UrlEncode(RSAParameters.DP ?? []);
            Dq = EncodingUtils.Base64UrlEncode(RSAParameters.DQ ?? []);
            E = EncodingUtils.Base64UrlEncode(RSAParameters.Exponent ?? []);
            D = EncodingUtils.Base64UrlEncode(RSAParameters.D ?? []);
            N = EncodingUtils.Base64UrlEncode(RSAParameters.Modulus ?? []);
            P = EncodingUtils.Base64UrlEncode(RSAParameters.P ?? []);
            Q = EncodingUtils.Base64UrlEncode(RSAParameters.Q ?? []);
            Qi = EncodingUtils.Base64UrlEncode(RSAParameters.InverseQ ?? []);
        }

        public JsonWebKeyModel(JsonWebKey key, string name, string version, HttpContext? reqContext)
        {
            var parameters = new RSAParameters
            {
                Modulus = EncodingUtils.Base64UrlDecode(key.N),
                Exponent = EncodingUtils.Base64UrlDecode(key.E),
                D = string.IsNullOrEmpty(key.D) ? null : EncodingUtils.Base64UrlDecode(key.D),
                P = string.IsNullOrEmpty(key.P) ? null : EncodingUtils.Base64UrlDecode(key.P),
                Q = string.IsNullOrEmpty(key.Q) ? null : EncodingUtils.Base64UrlDecode(key.Q),
                DP = string.IsNullOrEmpty(key.DP) ? null : EncodingUtils.Base64UrlDecode(key.DP),
                DQ = string.IsNullOrEmpty(key.DQ) ? null : EncodingUtils.Base64UrlDecode(key.DQ),
                InverseQ = string.IsNullOrEmpty(key.QI) ? null : EncodingUtils.Base64UrlDecode(key.QI)
            };

            RSAKey = RSA.Create(parameters);

            KeyType = key.Kty;

            var keyUrl = new UriBuilder
            {
                Scheme = reqContext?.Request.Scheme,
                Host = reqContext?.Request.Host.Host,
                Port = reqContext?.Request.Host.Port ?? -1,
                Path = $"keys/{name}/{version}"
            };

            D = key.D;
            Dp = key.DP;
            Dq = key.DQ;
            E = key.E;
            D = key.D;
            N = key.N;
            P = key.P;
            Q = key.Q;
            Qi = key.QI;

            KeyOperations = [.. key.KeyOps];
            KeyName = name;
            KeyVersion = version;
            KeyIdentifier = keyUrl.Uri.ToString();
        }

        public byte[] Encrypt(KeyOperationParameters data)
        {
            return data.Algorithm switch
            {
                EncryptionAlgorithms.RSA1_5 => RsaEncrypt(data.Data, RSAEncryptionPadding.Pkcs1),
                EncryptionAlgorithms.RSA_OAEP => RsaEncrypt(data.Data, RSAEncryptionPadding.OaepSHA1),
                EncryptionAlgorithms.RSA_OAEP_256 => RsaEncrypt(data.Data, RSAEncryptionPadding.OaepSHA256),
                _ => throw new NotImplementedException($"Algorithm '{data.Algorithm}' does not support Encryption")
            };
        }

        private byte[] RsaEncrypt(string plaintext, RSAEncryptionPadding padding)
        {
            using var rsaAlg = RSA.Create(RSAParameters);
            return rsaAlg.Encrypt(EncodingUtils.Base64UrlDecode(plaintext), padding);
        }

        public string Decrypt(KeyOperationParameters data)
        {
            return data.Algorithm switch
            {
                EncryptionAlgorithms.RSA1_5 => RsaDecrypt(data.Data, RSAEncryptionPadding.Pkcs1),
                EncryptionAlgorithms.RSA_OAEP => RsaDecrypt(data.Data, RSAEncryptionPadding.OaepSHA1),
                EncryptionAlgorithms.RSA_OAEP_256 => RsaDecrypt(data.Data, RSAEncryptionPadding.OaepSHA256),
                _ => throw new NotImplementedException($"Algorithm '{data.Algorithm}' does not support Decryption")
            };
        }

        private string RsaDecrypt(string ciphertext, RSAEncryptionPadding padding)
        {
            using var rsaAlg = RSA.Create(RSAParameters);
            return EncodingUtils.Base64UrlEncode(rsaAlg.Decrypt(EncodingUtils.Base64UrlDecode(ciphertext), padding));
        }

        public int GetKeySize() => RSAKey.KeySize;
    }
}

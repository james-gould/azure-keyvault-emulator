using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Keys.Constants;
using Microsoft.IdentityModel.Tokens;

namespace AzureKeyVaultEmulator.Keys.Models
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
            D = Base64UrlEncoder.Encode(_rsaParameters.D);
            Dp = Base64UrlEncoder.Encode(_rsaParameters.DP);
            Dq = Base64UrlEncoder.Encode(_rsaParameters.DQ);
            E = Base64UrlEncoder.Encode(_rsaParameters.Exponent);
            D = Base64UrlEncoder.Encode(_rsaParameters.D);
            KeyType = "RSA";
            N = Base64UrlEncoder.Encode(_rsaParameters.Modulus);
            P = Base64UrlEncoder.Encode(_rsaParameters.P);
            Q = Base64UrlEncoder.Encode(_rsaParameters.Q);
            Qi = Base64UrlEncoder.Encode(_rsaParameters.InverseQ);
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
            return rsaAlg.Encrypt(Base64UrlEncoder.DecodeBytes(plaintext), padding);
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
            return Base64UrlEncoder.Encode(rsaAlg.Decrypt(Base64UrlEncoder.DecodeBytes(ciphertext), padding));
        }
    }
}

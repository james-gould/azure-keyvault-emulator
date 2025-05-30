﻿using System.Security.Cryptography;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

namespace AzureKeyVaultEmulator.IntegrationTests.Keys;

public sealed class KeysControllerTests(KeysTestingFixture fixture) : IClassFixture<KeysTestingFixture>
{
    public static TheoryData<EncryptionAlgorithm> SupportedEncryptionAlgorithms => new TheoryData<EncryptionAlgorithm>
    {
        EncryptionAlgorithm.Rsa15,
        EncryptionAlgorithm.RsaOaep,
        EncryptionAlgorithm.RsaOaep256,
    };

    public static TheoryData<EncryptionAlgorithm, RSAEncryptionPadding> SupportedEncryptionAlgorithmsAndPadding => new TheoryData<EncryptionAlgorithm, RSAEncryptionPadding>
    {
        { EncryptionAlgorithm.Rsa15, RSAEncryptionPadding.Pkcs1 },
        { EncryptionAlgorithm.RsaOaep, RSAEncryptionPadding.OaepSHA1 },
        { EncryptionAlgorithm.RsaOaep256, RSAEncryptionPadding.OaepSHA256 },
    };

    [Fact]
    public async Task CreateAndGetKeySucceeds()
    {
        var client = await fixture.GetClientAsync();

        var name = fixture.FreshlyGeneratedGuid;

        var createdKey = await fixture.CreateKeyAsync(name);

        var keyFromStore = (await client.GetKeyAsync(name)).Value;

        Assert.KeysAreEqual(createdKey, keyFromStore);
    }

    [Fact]
    public async Task GetKeyWithVersionGetsCorrectVersion()
    {
        var client = await fixture.GetClientAsync();

        var versionedKeyName = "intTestKey";

        var firstKey = await fixture.CreateKeyAsync(versionedKeyName);
        var secondKey = await fixture.CreateKeyAsync(versionedKeyName);

        var firstFromStore = await client.GetKeyAsync(versionedKeyName, firstKey.Properties.Version);
        var secondFromStore = await client.GetKeyAsync(versionedKeyName, secondKey.Properties.Version);

        Assert.KeysAreEqual(firstKey, firstFromStore);
        Assert.KeysAreEqual(secondKey, secondFromStore);

        Assert.KeysNotEqual(firstKey, secondFromStore);
        Assert.KeysNotEqual(secondKey, firstFromStore);
    }

    [Fact]
    public async Task GetKeyThrowsWhenNoKeyExists()
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        await Assert.RequestFailsAsync(() => client.GetKeyAsync(keyName));
    }

    [Fact]
    public async Task CreatingKeyProvidesVersionByDefault()
    {
        var createdKey = await fixture.CreateKeyAsync("dummyKey");

        Assert.NotEqual(string.Empty, createdKey.Properties.Version);
    }

    [Fact]
    public async Task UpdateKeySetsPropertiesOnVersionedKey()
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;
        var tagKey = fixture.FreshlyGeneratedGuid;
        var tagValue = fixture.FreshlyGeneratedGuid;

        var createdKey = await fixture.CreateKeyAsync(keyName);

        Assert.True(createdKey.Properties?.Enabled);
        Assert.Empty(createdKey.Properties?.Tags);

        var newProps = new KeyProperties(keyName)
        {
            Enabled = false
        };

        newProps.Tags.Add(tagKey, tagValue);

        var disabledKeyWithTags = (await client.UpdateKeyPropertiesAsync(newProps)).Value;

        var shouldBeUpdated = (await client.GetKeyAsync(keyName, disabledKeyWithTags.Properties.Version)).Value;

        Assert.Equal(createdKey.Properties?.Version, shouldBeUpdated.Properties.Version);
        Assert.KeyHasTag(shouldBeUpdated, tagKey, tagValue);
    }

    [Fact]
    public async Task DeletingKeyWillRemoveItFromMainStore()
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var createdKey = await fixture.CreateKeyAsync(keyName);

        var keyFromMainStore = await client.GetKeyAsync(keyName);

        Assert.KeysAreEqual(createdKey, keyFromMainStore);

        var deletedKey = await client.StartDeleteKeyAsync(keyName);

        Assert.NotNull(deletedKey?.Value.DeletedOn);

        await Assert.RequestFailsAsync(() => client.GetKeyAsync(keyName));

        var fromDeletedStore = await client.GetDeletedKeyAsync(keyName);

        Assert.KeysAreEqual(createdKey, fromDeletedStore);
    }

    [Fact(Skip = "Cyclical tests randomly failing on Github, issue #145")]
    public async Task GetAllKeyVersionsWillCycle()
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var executionCount = await RequestSetup
            .CreateMultiple(26, 51, i => client.CreateKeyAsync(keyName, KeyType.Rsa));

        List<string> matchingKeys = [];

        await foreach (var key in client.GetPropertiesOfKeysAsync())
            if (!string.IsNullOrEmpty(key.Name) && key.Name.Contains(keyName))
                matchingKeys.Add(key.Name);

        Assert.Equal(executionCount, matchingKeys.Count);
    }

    [Fact(Skip = "Cyclical tests randomly failing on Github, issue #145")]
    public async Task GetOneHundredKeyVersionsCyclesThroughLink()
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var executionCount = await RequestSetup
            .CreateMultiple(26, 51, i => client.CreateKeyAsync(keyName, KeyType.Rsa));

        List<string> matchingKeys = [];

        await foreach (var key in client.GetPropertiesOfKeyVersionsAsync(keyName))
            if (!string.IsNullOrEmpty(key.Name) && key.Name.Contains(keyName))
                matchingKeys.Add(key.Name);

        Assert.Equal(executionCount, matchingKeys.Count);
    }

    [Theory]
    [MemberData(nameof(SupportedEncryptionAlgorithms))]
    public async Task EncryptAndDecryptWithKeySucceeds(EncryptionAlgorithm algo)
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var key = (await client.CreateKeyAsync(keyName, KeyType.Rsa)).Value;

        Assert.Equal(keyName, key.Name);

        var data = RequestSetup.CreateRandomBytes(128);

        var cryptoClient = client.GetCryptographyClient(keyName, key.Properties.Version);

        var encrypted = await cryptoClient.EncryptAsync(algo, data);

        Assert.NotEqual(data, encrypted.Ciphertext);

        var decrypted = await cryptoClient.DecryptAsync(algo, encrypted.Ciphertext);

        Assert.NotEqual(decrypted.Plaintext, encrypted.Ciphertext);
        Assert.Equal(decrypted.Plaintext, data);
    }

    [Theory]
    [MemberData(nameof(SupportedEncryptionAlgorithms))]
    public async Task EncryptAndDecryptWithKeyLatestVersionSucceeds(EncryptionAlgorithm algo)
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var key = (await client.CreateKeyAsync(keyName, KeyType.Rsa)).Value;

        Assert.Equal(keyName, key.Name);

        var data = RequestSetup.CreateRandomBytes(128);

        var cryptoClient = client.GetCryptographyClient(keyName, null); //null means the latest version

        var encrypted = await cryptoClient.EncryptAsync(algo, data);

        Assert.NotEqual(data, encrypted.Ciphertext);

        var decrypted = await cryptoClient.DecryptAsync(algo, encrypted.Ciphertext);

        Assert.NotEqual(decrypted.Plaintext, encrypted.Ciphertext);
        Assert.Equal(decrypted.Plaintext, data);
    }

    [Theory]
    [MemberData(nameof(SupportedEncryptionAlgorithms))]
    public async Task EncryptAndDecryptWithImportedKeySucceeds(EncryptionAlgorithm algo)
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        using var keyPair = RSA.Create();

        var jwk = new JsonWebKey(keyPair, true);

        var key = (await client.ImportKeyAsync(keyName, jwk)).Value;

        Assert.Equal(keyName, key.Name);

        var data = RequestSetup.CreateRandomBytes(128);

        var cryptoClient = client.GetCryptographyClient(keyName, key.Properties.Version);

        var encrypted = await cryptoClient.EncryptAsync(algo, data);

        Assert.NotEqual(data, encrypted.Ciphertext);

        var decrypted = await cryptoClient.DecryptAsync(algo, encrypted.Ciphertext);

        Assert.NotEqual(decrypted.Plaintext, encrypted.Ciphertext);
        Assert.Equal(decrypted.Plaintext, data);
    }

    [Theory]
    [MemberData(nameof(SupportedEncryptionAlgorithmsAndPadding))]
    public async Task EncryptWithPublicKeyAndDecryptWithVaultKeySucceeds(EncryptionAlgorithm algo, RSAEncryptionPadding encryptionPadding)
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var key = (await client.CreateKeyAsync(keyName, KeyType.Rsa)).Value;

        Assert.Equal(keyName, key.Name);

        var data = RequestSetup.CreateRandomBytes(128);

        var cryptoClient = client.GetCryptographyClient(keyName, key.Properties.Version);

        var publicKeyParameters = new RSAParameters()
        {
            Exponent = key.Key.E,
            Modulus = key.Key.N,
        };

        using var publicKey = RSA.Create(publicKeyParameters);

        var encrypted = publicKey.Encrypt(data, encryptionPadding);

        Assert.NotEqual(data, encrypted);

        var decrypted = await cryptoClient.DecryptAsync(algo, encrypted);

        Assert.NotEqual(decrypted.Plaintext, encrypted);
        Assert.Equal(decrypted.Plaintext, data);
    }

    [Theory]
    [MemberData(nameof(SupportedEncryptionAlgorithmsAndPadding))]
    public async Task EncryptWithPublicKeyAndDecryptWithImportedVaultKeySucceeds(EncryptionAlgorithm algo, RSAEncryptionPadding encryptionPadding)
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        using var keyPair = RSA.Create();

        var jwk = new JsonWebKey(keyPair, true);

        var key = (await client.ImportKeyAsync(keyName, jwk)).Value;

        Assert.Equal(keyName, key.Name);

        var data = RequestSetup.CreateRandomBytes(128);

        var cryptoClient = client.GetCryptographyClient(keyName, key.Properties.Version);

        var publicKeyParameters = new RSAParameters()
        {
            Exponent = key.Key.E,
            Modulus = key.Key.N,
        };

        using var publicKey = RSA.Create(publicKeyParameters);

        var encrypted = publicKey.Encrypt(data, encryptionPadding);

        Assert.NotEqual(data, encrypted);

        var decrypted = await cryptoClient.DecryptAsync(algo, encrypted);

        Assert.NotEqual(decrypted.Plaintext, encrypted);
        Assert.Equal(decrypted.Plaintext, data);
    }

    [Theory]
    [MemberData(nameof(SupportedEncryptionAlgorithmsAndPadding))]
    public async Task EncryptWithVaultKeyAndDecryptWithPrivateKeySucceeds(EncryptionAlgorithm algo, RSAEncryptionPadding encryptionPadding)
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        using var keyPair = RSA.Create();

        var jwk = new JsonWebKey(keyPair, true);

        var key = (await client.ImportKeyAsync(keyName, jwk)).Value;

        Assert.Equal(keyName, key.Name);
        Assert.Equal(jwk.E, key.Key.E);
        Assert.Equal(jwk.N, key.Key.N);

        var data = RequestSetup.CreateRandomBytes(128);

        var cryptoClient = client.GetCryptographyClient(keyName, key.Properties.Version);

        var encrypted = await cryptoClient.EncryptAsync(algo, data);

        Assert.NotEqual(data, encrypted.Ciphertext);

         var decrypted = keyPair.Decrypt(encrypted.Ciphertext, encryptionPadding);

        Assert.NotEqual(decrypted, encrypted.Ciphertext);
        Assert.Equal(decrypted, data);
    }

    [Fact]
    public async Task BackingUpAndRestoringKeySucceeds()
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var created = await fixture.CreateKeyAsync(keyName);

        var backedUpKey = (await client.BackupKeyAsync(keyName)).Value;

        Assert.NotEmpty(backedUpKey);

        var restoredKey = (await client.RestoreKeyBackupAsync(backedUpKey)).Value;

        Assert.KeysAreEqual(created, restoredKey);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(110)]
    [InlineData(120)]
    public async Task GetRandomBytesWillMatchRequestedLength(int length)
    {
        var client = await fixture.GetClientAsync();

        var bytes = await client.GetRandomBytesAsync(length);

        Assert.Equal(length, bytes?.Value?.Length);
    }

    [Fact]
    public async Task CreatingRotationPolicyWillPersistAgainstKey()
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var key = await fixture.CreateKeyAsync(keyName);

        await Assert.RequestFailsAsync(() => client.GetKeyRotationPolicyAsync(keyName));

        var policy = new KeyRotationPolicy
        {
            ExpiresIn = "P30M"
        };

        var createdPolicy = (await client.UpdateKeyRotationPolicyAsync(keyName, policy)).Value;

        Assert.Equal(policy.ExpiresIn, createdPolicy.ExpiresIn);

        var afterCreation = (await client.GetKeyRotationPolicyAsync(keyName)).Value;

        Assert.Equal(afterCreation.ExpiresIn, policy.ExpiresIn);
    }

    [Fact(Skip = "Release/Import key requires JWE decoding and inspection")]
    public async Task ReleasingKeyWillCreatePublicKeyHeader()
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var key = await fixture.CreateKeyAsync(keyName);

        ReleaseKeyOptions opt = new(keyName, "https://doesntmatter.com")
        {
            Version = key.Properties.Version
        };

        var releasedKey = (await client.ReleaseKeyAsync(opt)).Value;

        // TODO: requires JWE decoding and validation
        Assert.NotEqual(string.Empty, releasedKey.Value);

        // Requires JWE decoding
        //var imported = (await client.ImportKeyAsync(null)).Value;
    }

    [Fact]
    public async Task SignAndVerifyWithKeySucceeds()
    {
        // https://github.com/Azure/azure-sdk-for-net/blob/Azure.Security.KeyVault.Keys_4.7.0/sdk/keyvault/Azure.Security.KeyVault.Keys/samples/Sample5_SignVerify.md
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var key = await fixture.CreateKeyAsync(keyName);

        var cryptoProvider = await fixture.GetCryptographyClientAsync(key);

        var digest = RequestSetup.CreateRandomBytes(64);

        var signAlgorithm = SignatureAlgorithm.RS256;

        var signResult = await cryptoProvider.SignAsync(signAlgorithm, digest);

        var verifyResult = await cryptoProvider.VerifyAsync(signAlgorithm, digest, signResult.Signature);

        Assert.True(verifyResult.IsValid);
    }

    [Fact]
    public async Task SigningAndVerifyingWithDifferentKeysWillFail()
    {
        var client = await fixture.GetClientAsync();

        var signKeyName = fixture.FreshlyGeneratedGuid;
        var verifyKeyName = fixture.FreshlyGeneratedGuid;

        var signKey = await fixture.CreateKeyAsync(signKeyName);
        var verifyKey = await fixture.CreateKeyAsync(verifyKeyName);

        var signProvider = await fixture.GetCryptographyClientAsync(signKey);
        var verifyProvider = await fixture.GetCryptographyClientAsync(verifyKey);

        var data = RequestSetup.CreateRandomBytes(64);
        var digest = SHA256.HashData(data);

        var signAlgorithm = SignatureAlgorithm.RS256;

        var signResult = await signProvider.SignAsync(signAlgorithm, digest);

        var verifyResult = await verifyProvider.VerifyAsync(signAlgorithm, digest, signResult.Signature);

        Assert.False(verifyResult?.IsValid);
    }

    [Fact]
    public async Task WrappingAndUnwrappingKeyWillSucceed()
    {
        // https://github.com/Azure/azure-sdk-for-net/blob/Azure.Security.KeyVault.Keys_4.7.0/sdk/keyvault/Azure.Security.KeyVault.Keys/samples/Sample6_WrapUnwrap.md

        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var key = await fixture.CreateKeyAsync(keyName);

        var crypto = await fixture.GetCryptographyClientAsync(key);

        var unwrappedKey = Aes.Create().Key;

        var wrapAlgo = KeyWrapAlgorithm.RsaOaep;

        var wrappedResult = await crypto.WrapKeyAsync(wrapAlgo, unwrappedKey);

        Assert.NotEqual(unwrappedKey, wrappedResult.EncryptedKey);

        var unwrapResult = await crypto.UnwrapKeyAsync(wrapAlgo, wrappedResult.EncryptedKey);

        Assert.Equal(unwrappedKey, unwrapResult.Key);
    }
}

using Azure;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

namespace AzureKeyVaultEmulator.IntegrationTests.Keys;

public sealed class KeysControllerTests(KeysTestingFixture fixture) : IClassFixture<KeysTestingFixture>
{
    [Fact]
    public async Task CreateAndGetKeySucceeds()
    {
        var client = await fixture.GetKeyClientAsync();

        var createdKey = await fixture.CreateKeyAsync();

        var keyFromStore = (await client.GetKeyAsync(KeysTestingFixture.DefaultKeyName)).Value;

        Assert.KeysAreEqual(createdKey, keyFromStore);
    }

    [Fact]
    public async Task GetKeyWithVersionGetsCorrectVersion()
    {
        var client = await fixture.GetKeyClientAsync();

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
        var client = await fixture.GetKeyClientAsync();

        var keyName = fixture.FreshGeneratedGuid;

        var exception = await Assert.ThrowsAsync<RequestFailedException>(() => client.GetKeyAsync(keyName));

        Assert.Equal((int)HttpStatusCode.BadRequest, exception.Status);
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
        var client = await fixture.GetKeyClientAsync();

        var keyName = "updatedKey";
        var tagKey = fixture.FreshGeneratedGuid;
        var tagValue = fixture.FreshGeneratedGuid;

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
    public async Task GetOneHundredKeysCyclesThroughLink()
    {
        var client = await fixture.GetKeyClientAsync();

        var keyName = fixture.FreshGeneratedGuid;

        var executionCount = await RequestSetup
            .CreateMultiple(51, 300, i => client.CreateKeyAsync(keyName, KeyType.Rsa, cancellationToken: fixture.CancellationToken));

        List<string> matchingKeys = [];

        await foreach (var key in client.GetPropertiesOfKeysAsync(fixture.CancellationToken))
            if(!string.IsNullOrEmpty(key.Name) && key.Name.Contains(keyName))
                matchingKeys.Add(key.Name);

        Assert.Equal(executionCount + 1, matchingKeys.Count);
    }

    [Fact]
    public async Task GetOneHundredKeyVersionsCyclesThroughLink()
    {
        var client = await fixture.GetKeyClientAsync();

        var keyName = fixture.FreshGeneratedGuid;

        var executionCount = await RequestSetup
            .CreateMultiple(51, 300, i => client.CreateKeyAsync(keyName, KeyType.Rsa, cancellationToken: fixture.CancellationToken));

        List<string> matchingKeys = [];

        await foreach (var key in client.GetPropertiesOfKeyVersionsAsync(keyName))
            if (!string.IsNullOrEmpty(key.Name) && key.Name.Contains(keyName))
                matchingKeys.Add(key.Name);

        Assert.Equal(executionCount + 1, matchingKeys.Count);
    }

    [Fact]
    public async Task EncryptAndDecryptWithKeySucceeds()
    {
        var client = await fixture.GetKeyClientAsync();

        var keyName = fixture.FreshGeneratedGuid;

        var key = (await client.CreateKeyAsync(keyName, KeyType.Rsa, cancellationToken: fixture.CancellationToken)).Value;

        Assert.Equal(keyName, key.Name);

        var data = RequestSetup.CreateRandomBytes(128);

        var algo = EncryptionAlgorithm.RsaOaep;

        var cryptoClient = client.GetCryptographyClient(keyName, key.Properties.Version);

        var encrypted = await cryptoClient.EncryptAsync(algo, data, fixture.CancellationToken);

        Assert.NotEqual(data, encrypted.Ciphertext);

        var decrypted = await cryptoClient.DecryptAsync(algo, encrypted.Ciphertext, fixture.CancellationToken);

        Assert.NotEqual(decrypted.Plaintext, encrypted.Ciphertext);
        Assert.Equal(decrypted.Plaintext, data);
    }

    [Fact(Skip = "Weird bug with restore endpoint 404ing, works in swagger...")]
    public async Task BackingUpAndRestoringKeySucceeds()
    {
        var client = await fixture.GetKeyClientAsync();

        var keyName = fixture.FreshGeneratedGuid;

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
        var client = await fixture.GetKeyClientAsync();

        var bytes = await client.GetRandomBytesAsync(length);

        Assert.Equal(length, bytes?.Value?.Length);
    }

    [Fact]
    public async Task CreatingRotationPolicyWillPersistAgainstKey()
    {
        var client = await fixture.GetKeyClientAsync();

        var keyName = fixture.FreshGeneratedGuid;

        var key = await fixture.CreateKeyAsync(keyName);

        var exception = await Assert
            .ThrowsAsync<RequestFailedException>(() => client.GetKeyRotationPolicyAsync(keyName));

        Assert.Equal((int)HttpStatusCode.BadRequest, exception.Status);

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
        var client = await fixture.GetKeyClientAsync();

        var keyName = fixture.FreshGeneratedGuid;

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
}

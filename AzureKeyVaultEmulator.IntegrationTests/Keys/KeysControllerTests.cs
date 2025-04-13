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
    public async Task EncryptDataWillSucceed()
    {
        var client = await fixture.GetKeyClientAsync();

        var keyName = fixture.FreshGeneratedGuid;

        var key = (await client.CreateKeyAsync(keyName, KeyType.Rsa, cancellationToken: fixture.CancellationToken)).Value;

        Assert.Equal(keyName, key.Name);

        var data = RequestSetup.CreateRandomBytes(128);

        var encrypted = await client
            .GetCryptographyClient(keyName, key.Properties.Version)
            .EncryptAsync(EncryptionAlgorithm.RsaOaep, data, fixture.CancellationToken);

        Assert.Equal(key.Key.Id, encrypted.KeyId);
        Assert.NotEqual(encrypted.Ciphertext, data);
    }

    [Fact]
    public async Task DecryptDataWillReverseEncryption()
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
}

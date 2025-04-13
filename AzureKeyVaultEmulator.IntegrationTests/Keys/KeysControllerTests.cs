using Azure.Security.KeyVault.Keys;
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
        var tagKey = Guid.NewGuid().ToString("n");
        var tagValue = Guid.NewGuid().ToString("n");

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
}

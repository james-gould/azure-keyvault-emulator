using Azure.Security.KeyVault.Keys;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

namespace AzureKeyVaultEmulator.IntegrationTests.Keys;

public sealed class DeletedKeysControllerTests(KeysTestingFixture fixture) : IClassFixture<KeysTestingFixture>
{
    [Fact]
    public async Task GetDeletedKeyReturnsFromDeletedKeyStore()
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var createdKey = await fixture.CreateKeyAsync(keyName);

        var deletedKey = (await client.StartDeleteKeyAsync(keyName)).Value;

        await Assert.RequestFailsAsync(() => client.GetKeyAsync(keyName));

        var fromDeletedStore = await client.GetDeletedKeyAsync(keyName);

        Assert.KeysAreEqual(createdKey, deletedKey);
    }

    [Fact]
    public async Task GetDeletedKeysWillCycleLink()
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var executionCount = await
            RequestSetup.CreateMultiple(26, 30, y => client.CreateKeyAsync(keyName, KeyType.Rsa));

        var deleteOperation = (await client.StartDeleteKeyAsync(keyName)).Value;

        List<DeletedKey> detectedDeletedKeys = [];

        await foreach (var deletedKey in client.GetDeletedKeysAsync())
            if (deletedKey?.Name?.Equals(keyName, StringComparison.OrdinalIgnoreCase) == true)
                detectedDeletedKeys.Add(deletedKey);

        Assert.Equal(executionCount, detectedDeletedKeys.Count);
    }

    [Fact]
    public async Task PurgeDeletedKeyRemovedFromDeletedStore()
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var createdKey = await fixture.CreateKeyAsync(keyName);

        var deleteOperation = await client.StartDeleteKeyAsync(keyName);

        var deletedKey = await client.GetDeletedKeyAsync(keyName);

        Assert.KeysAreEqual(createdKey, deletedKey);

        var purgeResult = await client.PurgeDeletedKeyAsync(keyName);

        await Assert.RequestFailsAsync(() => client.GetDeletedKeyAsync(keyName));

        await Assert.RequestFailsAsync(() => client.GetKeyAsync(keyName));
    }

    [Fact]
    public async Task RestoreKeyRemovesFromDeletedStore()
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var createdKey = await fixture.CreateKeyAsync(keyName);

        await client.StartDeleteKeyAsync(keyName);

        var deletedKey = (await client.GetDeletedKeyAsync(keyName)).Value;

        Assert.KeysAreEqual(deletedKey, createdKey);

        var restoredKey = (await client.StartRecoverDeletedKeyAsync(keyName)).Value;

        Assert.KeysAreEqual(createdKey, restoredKey);

        await Assert.RequestFailsAsync(() => client.GetDeletedKeyAsync(keyName));

        var fromMainStore = (await client.GetKeyAsync(keyName)).Value;

        Assert.KeysAreEqual(createdKey, fromMainStore);
    }

    [Fact]
    public async Task RecoverDeletedKeyRestoresAllVersionsTest()
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var first = await fixture.CreateKeyAsync(keyName);
        var second = await fixture.CreateKeyAsync(keyName);

        await (await client.StartDeleteKeyAsync(keyName)).WaitForCompletionAsync();
        await (await client.StartRecoverDeletedKeyAsync(keyName)).WaitForCompletionAsync();

        var versions = new List<string>();
        await foreach (var version in client.GetPropertiesOfKeyVersionsAsync(keyName))
            versions.Add(version.Version);

        Assert.Equal(2, versions.Count);
        Assert.Contains(first.Properties.Version, versions);
        Assert.Contains(second.Properties.Version, versions);

        var olderVersion = (await client.GetKeyAsync(keyName, first.Properties.Version)).Value;
        Assert.KeysAreEqual(first, olderVersion);

        await Assert.RequestFailsAsync(() => client.GetDeletedKeyAsync(keyName));

        await foreach (var deleted in client.GetDeletedKeysAsync())
            Assert.NotEqual(keyName, deleted.Name);
    }
}

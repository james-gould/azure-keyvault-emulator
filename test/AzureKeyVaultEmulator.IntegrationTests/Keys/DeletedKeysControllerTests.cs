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

    [Fact(Skip = "Cyclical tests randomly failing on Github, issue #145")]
    public async Task GetDeletedKeysWillCycleLink()
    {
        var client = await fixture.GetClientAsync();

        var keyName = fixture.FreshlyGeneratedGuid;

        var executionCount = await
            RequestSetup.CreateMultiple(26, 51, y => client.CreateKeyAsync(keyName, KeyType.Rsa));

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
}

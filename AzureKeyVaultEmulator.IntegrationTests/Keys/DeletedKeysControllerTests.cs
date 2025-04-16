using Azure.Security.KeyVault.Keys;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

namespace AzureKeyVaultEmulator.IntegrationTests.Keys;

public sealed class DeletedKeysControllerTests(KeysTestingFixture fixture) : IClassFixture<KeysTestingFixture>
{
    [Fact]
    public async Task GetDeletedKeyReturnsFromDeletedKeyStore()
    {
        var client = await fixture.GetKeyClientAsync();

        var keyName = fixture.FreshGeneratedGuid;

        var createdKey = await fixture.CreateKeyAsync(keyName);

        var deletedKey = (await client.StartDeleteKeyAsync(keyName)).Value;

        await Assert.ThrowsRequestFailedAsync(() => client.GetKeyAsync(keyName));

        var fromDeletedStore = await client.GetDeletedKeyAsync(keyName);

        Assert.KeysAreEqual(createdKey, deletedKey);
    }

    [Fact]
    public async Task GetDeletedKeysWillCycleLink()
    {
        var client = await fixture.GetKeyClientAsync();

        var keyName = fixture.FreshGeneratedGuid;

        var executionCount = await
            RequestSetup.CreateMultiple(26, 51, y => client.CreateKeyAsync(keyName, KeyType.Rsa));

        var deleteOperation = (await client.StartDeleteKeyAsync(keyName)).Value;

        List<DeletedKey> detectedDeletedKeys = [];

        await foreach (var deletedKey in client.GetDeletedKeysAsync())
            if (deletedKey?.Name?.Equals(keyName, StringComparison.OrdinalIgnoreCase) == true)
                detectedDeletedKeys.Add(deletedKey);

        Assert.Equal(executionCount + 1, detectedDeletedKeys.Count);
    }

    [Fact]
    public async Task PurgeDeletedKeyRemovedFromDeletedStore()
    {
        var client = await fixture.GetKeyClientAsync();

        var keyName = fixture.FreshGeneratedGuid;

        var createdKey = await fixture.CreateKeyAsync(keyName);

        var deleteOperation = await client.StartDeleteKeyAsync(keyName);

        var deletedKey = await client.GetDeletedKeyAsync(keyName);

        Assert.KeysAreEqual(createdKey, deletedKey);

        var purgeResult = await client.PurgeDeletedKeyAsync(keyName);

        await Assert.ThrowsRequestFailedAsync(() => client.GetDeletedKeyAsync(keyName));

        await Assert.ThrowsRequestFailedAsync(() => client.GetKeyAsync(keyName));
    }
}

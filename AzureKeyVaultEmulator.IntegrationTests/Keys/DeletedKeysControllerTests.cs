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
}

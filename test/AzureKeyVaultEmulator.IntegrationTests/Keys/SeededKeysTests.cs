using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures.Seeding;
using AzureKeyVaultEmulator.Shared.Constants.Orchestration;

namespace AzureKeyVaultEmulator.IntegrationTests.Keys;

public sealed class SeededKeysTests(SeededKeysTestingFixture fixture) : IClassFixture<SeededKeysTestingFixture>
{
    [Fact]
    public async Task SeededKeyIsAvailableInRunningEmulatorTest()
    {
        var client = await fixture.GetClientAsync();

        var seededKey = await client.GetKeyAsync(SeedingConstants.SeededKeyName);

        Assert.NotNull(seededKey);
        Assert.NotNull(seededKey.Value);
        Assert.Equal(SeedingConstants.SeededKeyName, seededKey.Value.Name);
    }

    [Fact]
    public async Task SeededKeyHasConfiguredKeyTypeTest()
    {
        var client = await fixture.GetClientAsync();

        var seededKey = await client.GetKeyAsync(SeedingConstants.SeededKeyName);

        Assert.Equal(SeededKeysTestingFixture.SeededKeyType, seededKey.Value.KeyType);
        Assert.Equal(SeededKeysTestingFixture.SeededKeyType, seededKey.Value.Key.KeyType);
    }

    [Fact]
    public async Task SeededKeyWillHaveAttributesSetTest()
    {
        var client = await fixture.GetClientAsync();

        var seededKey = await client.GetKeyAsync(SeedingConstants.SeededKeyName);

        Assert.NotNull(seededKey.Value.Properties);
        Assert.NotNull(seededKey.Value.Properties.RecoveryLevel);
        Assert.NotNull(seededKey.Value.Properties.RecoverableDays);
        Assert.False(string.IsNullOrEmpty(seededKey.Value.Properties.Version));
    }
}

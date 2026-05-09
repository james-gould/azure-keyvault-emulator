using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures.Seeding;
using AzureKeyVaultEmulator.Shared.Constants.Orchestration;

namespace AzureKeyVaultEmulator.IntegrationTests.Secrets
{
    public class SeededSecretsTests(SeededSecretsTestingFixture fixture) : IClassFixture<SeededSecretsTestingFixture>
    {
        [Fact]
        public async Task SeededSecretIsAvailableInRunningEmulatorTest()
        {
            var client = await fixture.GetClientAsync();

            var seededSecret = await client.GetSecretAsync(SeedingConstants.SeededSecretName);

            Assert.NotNull(seededSecret);
            Assert.NotNull(seededSecret.Value);
            Assert.Equal(SeedingConstants.SeededSecretName, seededSecret.Value.Name);
        }

        [Fact]
        public async Task SeededSecretReturnsConfiguredValueTest()
        {
            var client = await fixture.GetClientAsync();

            var seededSecret = await client.GetSecretAsync(SeedingConstants.SeededSecretName);

            Assert.Equal(SeedingConstants.SeededSecretValue, seededSecret.Value.Value);
        }

        [Fact]
        public async Task SeededSecretWillHaveAttributesSetTest()
        {
            var client = await fixture.GetClientAsync();

            var seededSecret = await client.GetSecretAsync(SeedingConstants.SeededSecretName);

            Assert.NotNull(seededSecret.Value.Properties);
            Assert.NotNull(seededSecret.Value.Properties.RecoveryLevel);
            Assert.NotNull(seededSecret.Value.Properties.RecoverableDays);
            Assert.False(string.IsNullOrEmpty(seededSecret.Value.Properties.Version));
        }
    }
}

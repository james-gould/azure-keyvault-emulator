using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;
using System.Net.Http.Json;

namespace AzureKeyVaultEmulator.IntegrationTests.Secrets
{
    public class SecretsControllerTests(SecretsTestingFixture fixture) : IClassFixture<SecretsTestingFixture>
    {
        private readonly string _defaultSecretName = "password";
        private readonly string _defaultSecretValue = "myPassword";

        private KeyVaultSecret? _defaultSecret = null;

        [Fact]
        public async Task GetSecretReturnsCorrectValueTest()
        {
            var client = await fixture.GetSecretClientAsync();

            Assert.NotNull(client);

            var createdSecret = await CreateSecretAsync(client);

            var fromEmulator = await client.GetSecretAsync(_defaultSecretName);

            Assert.NotNull(fromEmulator);

            Assert.Equal(_defaultSecretValue, createdSecret.Value);
        }

        [Fact]
        public async Task SetSecretCreatesSecretInMemoryTest()
        {
            var client = await fixture.GetSecretClientAsync();

            Assert.NotNull(client);

            var createdSecret = await client.SetSecretAsync(new KeyVaultSecret(_defaultSecretName, _defaultSecretValue));

            Assert.NotNull(createdSecret.Value);
            Assert.False(createdSecret.GetRawResponse().IsError);

            var secretFromKv = await client.GetSecretAsync(_defaultSecretValue, createdSecret.Value.Properties.Version);

            Assert.Equal(createdSecret.Value.Value, secretFromKv.Value.Value);
            Assert.Equal(createdSecret.Value.Properties.Version, secretFromKv.Value.Properties.Version);
        }

        [Fact]
        public async Task GetSecretAfterDeletingProvidesKeyVaultErrorTest()
        {
            var client = await fixture.GetSecretClientAsync();
        }

        private async ValueTask<KeyVaultSecret> CreateSecretAsync(SecretClient client)
        {
            if (_defaultSecret is not null)
                return _defaultSecret;

            return _defaultSecret = await client.SetSecretAsync(new KeyVaultSecret(_defaultSecretName, _defaultSecretValue));
        }

        private async Task<KeyVaultSecret> CreateSecretAsync(SecretClient client, string name, string value)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            return await client.SetSecretAsync(new KeyVaultSecret(name, value));
        }
    }
}

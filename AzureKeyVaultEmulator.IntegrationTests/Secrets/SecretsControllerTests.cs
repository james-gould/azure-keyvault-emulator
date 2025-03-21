using Azure;
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

            var createdSecret = await CreateSecretAsync(client);

            var fromEmulator = await client.GetSecretAsync(_defaultSecretName);

            Assert.NotNull(fromEmulator);

            Assert.Equal(_defaultSecretValue, createdSecret.Value);
        }

        [Fact]
        public async Task SetSecretCreatesSecretInMemoryTest()
        {
            var client = await fixture.GetSecretClientAsync();

            var createdSecret = await client.SetSecretAsync(new KeyVaultSecret(_defaultSecretName, _defaultSecretValue));

            Assert.NotNull(createdSecret.Value);

            var secretFromKv = await client.GetSecretAsync(_defaultSecretName, createdSecret.Value.Properties.Version);

            Assert.Equal(createdSecret.Value.Value, secretFromKv.Value.Value);
            Assert.Equal(createdSecret.Value.Properties.Version, secretFromKv.Value.Properties.Version);
        }

        [Fact]
        public async Task GetSecretAfterDeletingProvidesKeyVaultErrorTest()
        {
            var client = await fixture.GetSecretClientAsync();

            var deletedName = "deletedSecret";
            var deletedValue = "iShouldBeDeleted";

            var createdSecret = await client.SetSecretAsync(new KeyVaultSecret(deletedName, deletedValue));

            Assert.NotNull(createdSecret.Value);

            var deletedSecret = await client.StartDeleteSecretAsync(deletedName);

            Assert.NotNull(deletedSecret.Value);
            Assert.Equal(deletedName, deletedSecret.Value.Name);

            var gottenAfterDeleted = await Assert.ThrowsAsync<RequestFailedException>(() => client.GetSecretAsync(deletedName));

            Assert.Equal((int)HttpStatusCode.BadRequest, gottenAfterDeleted.Status);
        }

        [Fact]
        public async Task BackupSecretAsyncReturnsEncodedNameTest()
        {
            var client = await fixture.GetSecretClientAsync();

            await CreateSecretAsync(client);

            var backup = await client.BackupSecretAsync(_defaultSecretName);

            Assert.NotEmpty(backup.Value);
        }

        [Fact]
        public async Task GetSecretVersionsReturnsBackMax25PerSetTest()
        {
            var client = await fixture.GetSecretClientAsync();

            var secretName = "multipleSecrets";
            var copies = 30;
            var versions = new List<string>();

            for(var i = 0; i < copies; i++)
            {
                await client.SetSecretAsync(secretName, $"{i}-value");
            }

            var properties = client.GetPropertiesOfSecretVersionsAsync(secretName);

            await foreach (var version in properties)
            {
                Assert.NotNull(version.Version);
                versions.Add(version.Version);
            }

            // Creating secret adds base secret + versioned one
            Assert.Equal(copies + 1, versions.Count);
        }

        private async ValueTask<KeyVaultSecret> CreateSecretAsync(SecretClient client)
        {
            if (_defaultSecret is not null)
                return _defaultSecret;

            return _defaultSecret = await client.SetSecretAsync(_defaultSecretName, _defaultSecretValue);
        }

        private async Task<KeyVaultSecret> CreateSecretAsync(SecretClient client, string name, string value)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            return await client.SetSecretAsync(name, value);
        }
    }
}

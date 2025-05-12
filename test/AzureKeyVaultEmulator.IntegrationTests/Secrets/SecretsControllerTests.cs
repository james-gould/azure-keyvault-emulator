using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

namespace AzureKeyVaultEmulator.IntegrationTests.Secrets
{
    public class SecretsControllerTests(SecretsTestingFixture fixture) : IClassFixture<SecretsTestingFixture>
    {
        [Fact]
        public async Task GetSecretReturnsCorrectValueTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;
            var secretValue = fixture.FreshlyGeneratedGuid;

            var createdSecret = await fixture.CreateSecretAsync(secretName, secretValue);

            var fromEmulator = await client.GetSecretAsync(secretName);

            Assert.NotNull(fromEmulator);

            Assert.Equal(secretValue, createdSecret.Value);
        }

        [Fact]
        public async Task SetSecretCreatesSecretInMemoryTest()
        {
            var client = await fixture.GetClientAsync();

            var name = fixture.FreshlyGeneratedGuid;
            var value = fixture.FreshlyGeneratedGuid;

            var createdSecret = await fixture.CreateSecretAsync(name, value);

            Assert.NotNull(createdSecret.Value);

            var secretFromKv = await client.GetSecretAsync(name, createdSecret.Properties.Version);

            Assert.Equal(createdSecret.Value, secretFromKv.Value.Value);
            Assert.Equal(createdSecret.Properties.Version, secretFromKv.Value.Properties.Version);
        }

        [Fact]
        public async Task GetSecretAfterDeletingProvidesKeyVaultErrorTest()
        {
            var client = await fixture.GetClientAsync();

            var deletedName = fixture.FreshlyGeneratedGuid;
            var deletedValue = fixture.FreshlyGeneratedGuid;

            var createdSecret = await fixture.CreateSecretAsync(deletedName, deletedValue);

            Assert.NotNull(createdSecret.Value);

            var deletedSecret = await client.StartDeleteSecretAsync(deletedName);

            Assert.NotNull(deletedSecret.Value);
            Assert.Equal(deletedName, deletedSecret.Value.Name);

            await Assert.RequestFailsAsync(() => client.GetSecretAsync(deletedName));

        }

        [Fact]
        public async Task BackupSecretAsyncReturnsEncodedNameTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;

            await fixture.CreateSecretAsync(secretName, fixture.FreshlyGeneratedGuid);

            var backup = await client.BackupSecretAsync(secretName);

            Assert.NotEmpty(backup.Value);
        }

        [Fact]
        public async Task GetSecretVersionsPagesAllSecretsByNameTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;

            var executionCount = await RequestSetup
                .CreateMultiple(26, 51, i => client.SetSecretAsync(secretName, $"{i}value"));

            var properties = client.GetPropertiesOfSecretVersionsAsync(secretName);

            var versions = new List<string>();

            await foreach (var version in properties)
            {
                Assert.NotEqual(string.Empty, version.Version);
                versions.Add(version.Version);
            }

            Assert.Equal(executionCount, versions.Count);
        }

        [Fact(Skip = "Cyclical tests randomly failing on Github, issue #145")]
        public async Task GetSecretsPagesAllSecretsCreatedTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;

            var executionCount = await RequestSetup
                .CreateMultiple(26, 51, i => client.SetSecretAsync(secretName, $"{i}value"));

            var testSecrets = new List<SecretProperties?>();

            var secrets = client.GetPropertiesOfSecretsAsync();

            await foreach (var secret in secrets)
                if (secret.Name.Equals(secretName, StringComparison.CurrentCultureIgnoreCase))
                    testSecrets.Add(secret);

            Assert.Equal(executionCount, testSecrets.Count);
        }

        [Fact]
        public async Task RestoreSecretTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;
            var secretValue = fixture.FreshlyGeneratedGuid;

            var secret = await fixture.CreateSecretAsync(secretName, secretValue);

            var backup = await client.BackupSecretAsync(secretName);

            Assert.NotNull(backup);

            var restored = await client.RestoreSecretBackupAsync(backup.Value);

            Assert.Equal(secret.Name, restored.Value.Name);
            Assert.Equal(secret.Id, restored.Value.Id);
        }

        [Fact]
        public async Task UpdateSecretAppliesChangesTest()
        {
            var client = await fixture.GetClientAsync();

            var secret = await fixture.CreateSecretAsync(fixture.FreshlyGeneratedGuid, fixture.FreshlyGeneratedGuid);

            Assert.True(string.IsNullOrEmpty(secret.Properties.ContentType));

            var newContentType = "application/json";

            secret.Properties.ContentType = newContentType;

            var updated = await client.UpdateSecretPropertiesAsync(secret.Properties);

            Assert.Equal(newContentType, updated.Value.ContentType);
        }

        [Fact]
        public async Task DeleteSecretBehavesCorrectlyTest()
        {
            var client = await fixture.GetClientAsync();

            var secret = await fixture.CreateSecretAsync(fixture.FreshlyGeneratedGuid, fixture.FreshlyGeneratedGuid);

            var deletedOperation = await client.StartDeleteSecretAsync(secret.Name);

            Assert.Equal(deletedOperation.Value.Value, secret.Value);

            await Assert.RequestFailsAsync(() => client.GetSecretAsync(secret.Name));
        }
    }
}

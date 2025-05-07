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

            var createdSecret = await fixture.CreateSecretAsync();

            var fromEmulator = await client.GetSecretAsync(fixture.DefaultSecretName);

            Assert.NotNull(fromEmulator);

            Assert.Equal(fixture.DefaultSecretValue, createdSecret.Value);
        }

        [Fact]
        public async Task SetSecretCreatesSecretInMemoryTest()
        {
            var client = await fixture.GetClientAsync();

            var createdSecret = await client.SetSecretAsync(new KeyVaultSecret(fixture.DefaultSecretName, fixture.DefaultSecretValue));

            Assert.NotNull(createdSecret.Value);

            var secretFromKv = await client.GetSecretAsync(fixture.DefaultSecretName, createdSecret.Value.Properties.Version);

            Assert.Equal(createdSecret.Value.Value, secretFromKv.Value.Value);
            Assert.Equal(createdSecret.Value.Properties.Version, secretFromKv.Value.Properties.Version);
        }

        [Fact]
        public async Task GetSecretAfterDeletingProvidesKeyVaultErrorTest()
        {
            var client = await fixture.GetClientAsync();

            var deletedName = "deletedSecret";
            var deletedValue = "iShouldBeDeleted";

            var createdSecret = await client.SetSecretAsync(new KeyVaultSecret(deletedName, deletedValue));

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

            var secretName = "myBackedUpSecret";

            await fixture.CreateSecretAsync(secretName, "doesntmatter");

            var backup = await client.BackupSecretAsync(secretName);

            Assert.NotEmpty(backup.Value);
        }

        [Fact]
        public async Task GetSecretVersionsPagesAllSecretsByNameTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = "multipleSecrets";

            var executionCount = await RequestSetup
                .CreateMultiple(26, 51, i => client.SetSecretAsync(secretName, $"{i}value"));

            var properties = client.GetPropertiesOfSecretVersionsAsync(secretName);

            var versions = new List<string>();

            await foreach (var version in properties)
            {
                Assert.NotEqual(string.Empty, version.Version);
                versions.Add(version.Version);
            }

            // Creating secret adds base secret + versioned one
            Assert.Equal(executionCount + 1, versions.Count);
        }

        [Fact(Skip = "Cyclical tests randomly failing on Github, issue #145")]
        public async Task GetSecretsPagesAllSecretsCreatedTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = "mulitudeTesting";

            var executionCount = await RequestSetup
                .CreateMultiple(26, 51, i => client.SetSecretAsync(secretName, $"{i}value"));

            var testSecrets = new List<SecretProperties?>();

            var secrets = client.GetPropertiesOfSecretsAsync();

            await foreach (var secret in secrets)
                if (secret.Name.Equals(secretName, StringComparison.CurrentCultureIgnoreCase))
                    testSecrets.Add(secret);

            Assert.Equal(executionCount + 1, testSecrets.Count);
        }

        [Fact]
        public async Task RestoreSecretTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = "restoringSecretName";
            var secretValue = "shouldBeRestored";

            var secret = await fixture.CreateSecretAsync(secretName, secretValue);

            var backup = await client.BackupSecretAsync(secretName);

            var restored = await client.RestoreSecretBackupAsync(backup.Value);

            Assert.Equal(secret.Name, restored.Value.Name);
            Assert.Equal(secret.Id, restored.Value.Id);
        }

        [Fact]
        public async Task UpdateSecretAppliesChangesTest()
        {
            var client = await fixture.GetClientAsync();

            var secret = await fixture.CreateSecretAsync();

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

            var secret = await fixture.CreateSecretAsync();

            var deletedOperation = await client.StartDeleteSecretAsync(secret.Name);

            Assert.Equal(deletedOperation.Value.Value, secret.Value);

            await Assert.RequestFailsAsync(() => client.GetSecretAsync(secret.Name));
        }
    }
}

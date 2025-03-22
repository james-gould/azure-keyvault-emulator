using Azure;
using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

namespace AzureKeyVaultEmulator.IntegrationTests.Secrets
{
    public class SecretsControllerTests(SecretsTestingFixture fixture) : IClassFixture<SecretsTestingFixture>
    {
        [Fact]
        public async Task GetSecretReturnsCorrectValueTest()
        {
            var client = await fixture.GetSecretClientAsync();

            var createdSecret = await fixture.CreateSecretAsync();

            var fromEmulator = await client.GetSecretAsync(fixture.DefaultSecretName);

            Assert.NotNull(fromEmulator);

            Assert.Equal(fixture.DefaultSecretValue, createdSecret.Value);
        }

        [Fact]
        public async Task SetSecretCreatesSecretInMemoryTest()
        {
            var client = await fixture.GetSecretClientAsync();

            var createdSecret = await client.SetSecretAsync(new KeyVaultSecret(fixture.DefaultSecretName, fixture.DefaultSecretValue));

            Assert.NotNull(createdSecret.Value);

            var secretFromKv = await client.GetSecretAsync(fixture.DefaultSecretName, createdSecret.Value.Properties.Version);

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

            var secretName = "myBackedUpSecret";

            await fixture.CreateSecretAsync(secretName, "doesntmatter");

            var backup = await client.BackupSecretAsync(secretName);

            Assert.NotEmpty(backup.Value);
        }

        [Fact]
        public async Task GetSecretVersionsPagesAllSecretsByNameTest()
        {
            var client = await fixture.GetSecretClientAsync();

            var secretName = "multipleSecrets";
            var copies = Random.Shared.Next(30, 100);
            var tasks = Enumerable.Range(0, copies)
                                  .Select(i => client.SetSecretAsync(secretName, $"{i}value", fixture.CancellationToken));

            await Task.WhenAll(tasks);

            var properties = client.GetPropertiesOfSecretVersionsAsync(secretName, fixture.CancellationToken);

            var versions = new List<string>();

            await foreach (var version in properties)
            {
                Assert.NotEqual(string.Empty, version.Version);
                versions.Add(version.Version);
            }

            // Creating secret adds base secret + versioned one
            Assert.Equal(copies + 1, versions.Count);
        }

        [Fact]
        public async Task GetSecretsPagesAllSecretsCreatedTest()
        {
            var client = await fixture.GetSecretClientAsync();

            var multipleCount = Random.Shared.Next(51, 300);
            var secretName = "mulitudeTesting";
            var tasks = Enumerable.Range(0, multipleCount)
                                  .Select(i => client.SetSecretAsync(secretName, $"{i}value", fixture.CancellationToken));

            await Task.WhenAll(tasks);

            var testSecrets = new List<SecretProperties?>();

            var secrets = client.GetPropertiesOfSecretsAsync(fixture.CancellationToken);

            await foreach (var secret in secrets)
                if (secret.Name.Equals(secretName, StringComparison.CurrentCultureIgnoreCase))
                    testSecrets.Add(secret);

            Assert.Equal(multipleCount + 1, testSecrets.Count);
        }

        [Fact]
        public async Task RestoreSecretTest()
        {
            var client = await fixture.GetSecretClientAsync();

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
            var client = await fixture.GetSecretClientAsync();

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
            var client = await fixture.GetSecretClientAsync();

            var secret = await fixture.CreateSecretAsync();

            var deletedOperation = await client.StartDeleteSecretAsync(secret.Name);

            Assert.Equal(deletedOperation.Value.Value, secret.Value);

            var result = await Assert.ThrowsAsync<RequestFailedException>(() => client.GetSecretAsync(secret.Name));

            Assert.Equal((int)HttpStatusCode.BadRequest, result.Status);
        }
    }
}

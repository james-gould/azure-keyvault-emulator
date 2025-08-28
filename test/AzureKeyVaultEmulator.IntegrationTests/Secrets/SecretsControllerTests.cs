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
        public async Task SecretWillHaveAttributesSet()
        {
            var secretName = fixture.FreshlyGeneratedGuid;

            var secretValue = fixture.FreshlyGeneratedGuid;

            var secret = await fixture.CreateSecretAsync(secretName, secretValue);

            Assert.NotNull(secret);
            Assert.NotNull(secret.Properties.RecoveryLevel);
            Assert.NotNull(secret.Properties.RecoverableDays);
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
                .CreateMultiple(26, 30, i => client.SetSecretAsync(secretName, $"{i}value"));

            var properties = client.GetPropertiesOfSecretVersionsAsync(secretName);

            var versions = new List<string>();

            await foreach (var version in properties)
            {
                Assert.NotEqual(string.Empty, version.Version);
                versions.Add(version.Version);
            }

            Assert.Equal(executionCount, versions.Count);
        }
    
        [Fact]
        public async Task GetSecretsPagesAllSecretsCreatedTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;

            var executionCount = await RequestSetup
                .CreateMultiple(26, 30, i => client.SetSecretAsync(secretName, $"{i}value"));

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

        [Fact]
        public async Task DeletedKeyWillBeMissedInAllKeys()
        {
            var client = await fixture.GetClientAsync();

            var secretNameToDelete = fixture.FreshlyGeneratedGuid;
            var secretNamesToKeep = fixture.FreshlyGeneratedGuid;

            var secretValue = fixture.FreshlyGeneratedGuid;

            var min = 1;
            var max = 3;

            var createdCount = await RequestSetup.CreateMultiple(min, max, x => fixture.CreateSecretAsync(secretNamesToKeep, secretValue));

            var key = await fixture.CreateSecretAsync(secretNameToDelete, secretValue);

            Assert.NotNull(key);
            Assert.Equal(secretNameToDelete, key.Name);

            var deletedSecretOp = await client.StartDeleteSecretAsync(secretNameToDelete);

            await deletedSecretOp.WaitForCompletionAsync();

            var inDeletedStore = await client.GetDeletedSecretAsync(secretNameToDelete);

            Assert.NotNull(inDeletedStore.Value);
            Assert.Equal(secretNameToDelete, inDeletedStore.Value.Name);

            await Assert.RequestFailsAsync(() => client.GetSecretAsync(secretNameToDelete));

            var allUndeletedSecrets = client.GetPropertiesOfSecretsAsync();

            var existingCount = 0;

            await foreach (var existingSecret in allUndeletedSecrets)
            {
                Assert.NotEqual(secretNameToDelete, existingSecret.Name);

                if(existingSecret.Name.Equals(secretNamesToKeep, StringComparison.InvariantCultureIgnoreCase))
                    existingCount++;
            }

            Assert.Equal(createdCount, existingCount);
        }

        [Fact]
        public async Task CreatingUpdatingAndDeletePurgingSecretWillFlowCorrectly()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;
            var secretValue = fixture.FreshlyGeneratedGuid;

            var createdSecret = await fixture.CreateSecretAsync(secretName, secretValue);

            Assert.Equal(createdSecret.Name, secretName);
            Assert.Equal(createdSecret.Value, secretValue);

            var contentType = "application/text";

            createdSecret.Properties.ContentType = contentType;

            await client.UpdateSecretPropertiesAsync(createdSecret.Properties);

            var updatedSecretResponse = await client.GetSecretAsync(secretName);

            var updatedSecret = updatedSecretResponse.Value;

            Assert.Equal(contentType, updatedSecret.Properties.ContentType);

            var deleteOperation = await client.StartDeleteSecretAsync(secretName);

            await deleteOperation.WaitForCompletionAsync();

            await Assert.RequestFailsAsync(() => client.GetSecretAsync(secretName));

            var deletedSecretResponse = await client.GetDeletedSecretAsync(secretName);

            var deletedSecret = deletedSecretResponse.Value;

            Assert.Equal(deletedSecret.Name, secretName);
            Assert.Equal(deletedSecret.Value, secretValue);

            await client.PurgeDeletedSecretAsync(secretName);

            await Assert.RequestFailsAsync(() => client.GetDeletedSecretAsync(secretName));
            await Assert.RequestFailsAsync(() => client.GetSecretAsync(secretName));
        }
    }
}

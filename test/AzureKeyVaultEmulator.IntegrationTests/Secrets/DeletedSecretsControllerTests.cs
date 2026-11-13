using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

namespace AzureKeyVaultEmulator.IntegrationTests.Secrets
{
    public class DeletedSecretsControllerTests(SecretsTestingFixture fixture) : IClassFixture<SecretsTestingFixture>
    {
        [Fact]
        public async Task GetDeletedSecretReturnsPreviousDeletedSecretTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;
            var secretValue = fixture.FreshlyGeneratedGuid;

            var secret = await fixture.CreateSecretAsync(secretName, secretValue);

            var deletedAction = await client.StartDeleteSecretAsync(secret.Name);

            Assert.Equal(secret.Name, deletedAction.Value.Name);

            await Assert.RequestFailsAsync(() => client.GetSecretAsync(secret.Name));
           
            var fromDeletedSource = await client.GetDeletedSecretAsync(secret.Name);

            Assert.Equal(secret.Name, fromDeletedSource.Value.Name);
        }

        [Fact]
        public async Task GetDeletedSecretsPagesForCorrectCountTest()
        {
            var client = await fixture.GetClientAsync();

            var multiSecretName = fixture.FreshlyGeneratedGuid;

            var executionCount = await RequestSetup
                .CreateMultiple(26, 30, i => client.SetSecretAsync(multiSecretName, $"{i}value"));

            var deletedOperation = await client.StartDeleteSecretAsync(multiSecretName);

            Assert.Equal(multiSecretName, deletedOperation.Value.Name);

            var deletedSecrets = new List<DeletedSecret>();

            var deletePager = client.GetDeletedSecretsAsync();

            await foreach (var deletedSecret in deletePager)
                if (deletedSecret.Name.Contains(multiSecretName, StringComparison.CurrentCultureIgnoreCase))
                    deletedSecrets.Add(deletedSecret);

            Assert.Equal(executionCount, deletedSecrets.Count);
        }

        [Fact]
        public async Task PurgeDeletedSecretRemovesFromDeletedCacheTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;
            var secretValue = fixture.FreshlyGeneratedGuid;

            var secret = await fixture.CreateSecretAsync(secretName, secretValue);

            Assert.Equal(secretName, secret.Name);

            var deletedOperation = await client.StartDeleteSecretAsync(secretName);

            Assert.Equal(secretName, deletedOperation?.Value.Name);

            var deletedSecretFromCache = await client.GetDeletedSecretAsync(secretName);

            Assert.Equal(secretName, deletedSecretFromCache?.Value.Name);

            await client.PurgeDeletedSecretAsync(secretName);

            await Assert.RequestFailsAsync(() => client.GetDeletedSecretAsync(secretName));

            await Assert.RequestFailsAsync(() => client.GetSecretAsync(secretName));
        }

        [Fact]
        public async Task RecoverDeletedSecretRestoresToPrimaryCacheTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;
            var secretValue = fixture.FreshlyGeneratedGuid;

            var secret = await fixture.CreateSecretAsync(secretName, secretValue);

            Assert.Equal(secretName, secret.Name);

            var deletedOperation = await client.StartDeleteSecretAsync(secretName);

            Assert.Equal(secretName, deletedOperation.Value.Name);

            var recovered = await client.StartRecoverDeletedSecretAsync(secretName);

            Assert.Equal(secretName, recovered.Value.Name);

            var afterRecovery = await client.GetSecretAsync(secretName);

            Assert.Equal(secretName, afterRecovery.Value.Name);

            await Assert.RequestFailsAsync(() => client.GetDeletedSecretAsync(secretName));
        }

        [Fact]
        public async Task RecoverDeletedSecretRestoresAllVersionsTest()
        {
            var client = await fixture.GetClientAsync();

            var secretName = fixture.FreshlyGeneratedGuid;

            var first = await fixture.CreateSecretAsync(secretName, fixture.FreshlyGeneratedGuid);
            var second = await fixture.CreateSecretAsync(secretName, fixture.FreshlyGeneratedGuid);

            await (await client.StartDeleteSecretAsync(secretName)).WaitForCompletionAsync();
            await (await client.StartRecoverDeletedSecretAsync(secretName)).WaitForCompletionAsync();

            var versions = new List<string>();
            await foreach (var version in client.GetPropertiesOfSecretVersionsAsync(secretName))
                versions.Add(version.Version);

            Assert.Equal(2, versions.Count);
            Assert.Contains(first.Properties.Version, versions);
            Assert.Contains(second.Properties.Version, versions);

            var olderVersion = await client.GetSecretAsync(secretName, first.Properties.Version);
            Assert.Equal(first.Value, olderVersion.Value.Value);

            await Assert.RequestFailsAsync(() => client.GetDeletedSecretAsync(secretName));

            await foreach (var deleted in client.GetDeletedSecretsAsync())
                Assert.NotEqual(secretName, deleted.Name);
        }
    }
}

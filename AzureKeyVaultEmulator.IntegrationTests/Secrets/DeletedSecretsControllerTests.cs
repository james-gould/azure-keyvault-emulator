using Azure;
using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;
using Json.Patch;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace AzureKeyVaultEmulator.IntegrationTests.Secrets
{
    public class DeletedSecretsControllerTests(SecretsTestingFixture fixture) : IClassFixture<SecretsTestingFixture>
    {
        [Fact]
        public async Task GetDeletedSecretReturnsPreviousDeletedSecretTest()
        {
            var client = await fixture.GetSecretClientAsync();

            var secretName = "getDeletedSecretName";
            var secretValue = "getSelectedSecretPassword";

            var secret = await fixture.CreateSecretAsync(secretName, secretValue);

            var deletedAction = await client.StartDeleteSecretAsync(secret.Name);

            Assert.Equal(secret.Name, deletedAction.Value.Name);

            var shouldFail = await Assert.ThrowsAsync<RequestFailedException>(() => client.GetSecretAsync(secret.Name));

            Assert.Equal((int)HttpStatusCode.BadRequest, shouldFail.Status);

            var fromDeletedSource = await client.GetDeletedSecretAsync(secret.Name);

            Assert.Equal(secret.Name, fromDeletedSource.Value.Name);
        }

        [Fact]
        public async Task GetDeletedSecretsPagesForCorrectCountTest()
        {
            var client = await fixture.GetSecretClientAsync();

            var multiSecretName = "multiDelete";
            var multitude = Random.Shared.Next(30, 100);

            var tasks = Enumerable.Range(0, multitude).Select(i => client.SetSecretAsync(multiSecretName, $"{i}value"));

            await Task.WhenAll(tasks);

            var deletedOperation = await client.StartDeleteSecretAsync(multiSecretName);

            Assert.Equal(multiSecretName, deletedOperation.Value.Name);

            var deletedSecrets = new List<DeletedSecret>();

            var deletePager = client.GetDeletedSecretsAsync(fixture.CancellationToken);

            await foreach (var deletedSecret in deletePager)
                if (deletedSecret.Name.Contains(multiSecretName, StringComparison.CurrentCultureIgnoreCase))
                    deletedSecrets.Add(deletedSecret);

            Assert.Single(deletedSecrets);
        }

        [Fact]
        public async Task PurgeDeletedSecretRemovesFromDeletedCacheTest()
        {
            var client = await fixture.GetSecretClientAsync();

            var secretName = "purgingSecret";
            var secretValue = "purgedValue";

            var secret = await fixture.CreateSecretAsync(secretName, secretValue);

            Assert.Equal(secretName, secret.Name);

            var deletedOperation = await client.StartDeleteSecretAsync(secretName);

            Assert.Equal(secretName, deletedOperation?.Value.Name);

            var deletedSecretFromCache = await client.GetDeletedSecretAsync(secretName);

            Assert.Equal(secretName, deletedSecretFromCache?.Value.Name);

            await client.PurgeDeletedSecretAsync(secretName);

            var deletedEndpointResult = await Assert.ThrowsAsync<RequestFailedException>(() => client.GetDeletedSecretAsync(secretName));
            var baseEndpointResult = await Assert.ThrowsAsync<RequestFailedException>(() => client.GetSecretAsync(secretName));

            Assert.Equal((int)HttpStatusCode.BadRequest, deletedEndpointResult.Status);
            Assert.Equal((int)HttpStatusCode.BadRequest, baseEndpointResult.Status);
        }

        [Fact]
        public async Task RecoverDeletedSecretRestoresToPrimaryCacheTest()
        {
            var client = await fixture.GetSecretClientAsync();

            var secretName = "recoveredDeletedSecret";
            var secretValue = "recoveredPassword";

            var secret = await fixture.CreateSecretAsync(secretName, secretValue);

            Assert.Equal(secretName, secret.Name);

            var deletedOperation = await client.StartDeleteSecretAsync(secretName);

            Assert.Equal(secretName, deletedOperation.Value.Name);

            var recovered = await client.StartRecoverDeletedSecretAsync(secretName);

            Assert.Equal(secretName, recovered.Value.Name);

            var afterRecovery = await client.GetSecretAsync(secretName);

            Assert.Equal(secretName, afterRecovery.Value.Name);

            var deletedResult = await Assert.ThrowsAsync<RequestFailedException>(()=> client.GetDeletedSecretAsync(secretName));

            Assert.Equal((int)HttpStatusCode.BadRequest, deletedResult.Status);
        }
    }
}

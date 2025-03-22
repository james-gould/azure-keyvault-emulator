using Azure;
using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;
using System.Runtime.InteropServices;

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
    }
}

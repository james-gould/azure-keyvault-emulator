using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;
using System.Net.Http.Json;

namespace AzureKeyVaultEmulator.IntegrationTests.Secrets
{
    public class GetSecretTests(EmulatorTestingFixture fixture) : IClassFixture<EmulatorTestingFixture>
    {
        private readonly string _defaultSecretName = "password";
        private readonly string _defaultSecretValue = "myPassword";

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

        private async Task<KeyVaultSecret> CreateSecretAsync(SecretClient client, string name = "", string value = "")
        {
            if (string.IsNullOrEmpty(name))
                name = _defaultSecretName;

            if (string.IsNullOrEmpty(value))
                value = _defaultSecretValue;

            return await client.SetSecretAsync(new KeyVaultSecret(name, value));
        }
    }
}

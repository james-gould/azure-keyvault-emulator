using AzureKeyVaultEmulator.IntegrationTests.SetupHelper;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;
using System.Net.Http.Json;

namespace AzureKeyVaultEmulator.IntegrationTests.Secrets
{
    public class GetSecretTests(EmulatorTestingFixture fixture) : IClassFixture<EmulatorTestingFixture>
    {
        private readonly string _defaultSecretName = "password";
        private readonly string _defaultSecretValue = "myPassword";

        [Theory]
        [InlineData(1.0)]
        public async Task GetSecretReturnsCorrectValueTest(double version)
        {
            var client = fixture.CreateHttpClient(version);

            Assert.NotNull(client);

            await CreateSecretAsync(client);

            var response = await client.GetAsync($"secrets/{_defaultSecretName}");

            response.EnsureSuccessStatusCode();

            var secret = await response.Content.ReadFromJsonAsync<SecretResponse>();

            Assert.NotNull(secret);

            Assert.Equal(_defaultSecretValue, secret.Value);
        }

        private async Task CreateSecretAsync(HttpClient client, string name = "", string value = "")
        {
            if (string.IsNullOrEmpty(name))
                name = _defaultSecretName;

            if (string.IsNullOrEmpty(value))
                value = _defaultSecretValue;

            var createdSecret = RequestSetup.CreateSecretModel(value);

            var createResponse = await client.PutAsync($"secrets/{name}", createdSecret);

            createResponse.EnsureSuccessStatusCode();
        }
    }
}

using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace AzureKeyVaultEmulator.IntegrationTests.Secrets
{
    public class GetSecretTests(EmulatorTestingFixture fixture) : IClassFixture<EmulatorTestingFixture>
    {
        [Theory]
        [InlineData(1.0)]
        public async Task GetSecretReturnsCorrectValueTest(double version)
        {
            var client = fixture.CreateHttpClient(version);

            await Task.CompletedTask;

            Assert.NotNull(client);
        }
    }
}

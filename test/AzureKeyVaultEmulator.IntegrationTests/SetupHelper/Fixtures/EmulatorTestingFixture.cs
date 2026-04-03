namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

public class EmulatorTestingFixture : KeyVaultClientTestingFixture<HttpClient>
{
    public override async ValueTask<HttpClient> GetClientAsync()
    {
        return await CreateHttpClient();
    }
}

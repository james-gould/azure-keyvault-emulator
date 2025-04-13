using Azure.Security.KeyVault.Keys;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

public sealed class KeysTestingFixture : EmulatorTestingFixture
{
    private KeyClient? _client;

    public async ValueTask<KeyClient> GetKeyClientAsync()
    {
        if (_client is not null)
            return _client;

        var setup = await GetClientSetupModelAsync();

        var opt = new KeyClientOptions
        {
            DisableChallengeResourceVerification = true
        };

         return _client = new KeyClient(setup.VaultUri, setup.Credential, opt);
    }
}

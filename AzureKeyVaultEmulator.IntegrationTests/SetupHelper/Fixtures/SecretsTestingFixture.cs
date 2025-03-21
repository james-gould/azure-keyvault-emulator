using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.Shared.Constants;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures
{
    public class SecretsTestingFixture : EmulatorTestingFixture
    {
        private SecretClient? _secretClient;

        public async ValueTask<SecretClient> GetSecretClientAsync(string applicationName = AspireConstants.EmulatorServiceName)
        {
            if (_secretClient is not null)
                return _secretClient;

            var vaultEndpoint = _app!.GetEndpoint(applicationName);

            await _notificationService!.WaitForResourceAsync(applicationName).WaitAsync(_waitPeriod);

            var options = new SecretClientOptions
            {
                DisableChallengeResourceVerification = true
            };

            var emulatedBearerToken = await GetBearerToken();

            var cred = new EmulatedTokenCredential(emulatedBearerToken);

            _secretClient = new SecretClient(vaultEndpoint, cred, options);

            return _secretClient;
        }
    }
}

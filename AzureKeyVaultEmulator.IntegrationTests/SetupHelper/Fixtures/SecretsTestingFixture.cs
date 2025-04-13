using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.Shared.Constants;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures
{
    public class SecretsTestingFixture : EmulatorTestingFixture
    {
        private SecretClient? _secretClient;
        private CancellationTokenSource _cancellationTokenSource = new(TimeSpan.FromSeconds(30));

        public readonly string DefaultSecretName = "password";
        public readonly string DefaultSecretValue = "hunter2";
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        private KeyVaultSecret? _defaultSecret = null;

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

        public async ValueTask<KeyVaultSecret> CreateSecretAsync()
        {
            if (_defaultSecret is not null)
                return _defaultSecret;

            ArgumentNullException.ThrowIfNull(_secretClient);

            return _defaultSecret = await _secretClient.SetSecretAsync(DefaultSecretName, DefaultSecretValue);
        }

        public async Task<KeyVaultSecret> CreateSecretAsync(string secretName, string secretValue)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(secretName);
            ArgumentException.ThrowIfNullOrWhiteSpace(secretValue);
            ArgumentNullException.ThrowIfNull(_secretClient);

            return await _secretClient.SetSecretAsync(secretName, secretValue, CancellationToken);
        }
    }
}

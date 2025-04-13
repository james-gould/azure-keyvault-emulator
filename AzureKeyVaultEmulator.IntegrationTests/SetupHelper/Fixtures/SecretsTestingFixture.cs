using Azure.Security.KeyVault.Secrets;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

public class SecretsTestingFixture : EmulatorTestingFixture
{
    private SecretClient? _secretClient;
    private CancellationTokenSource _cancellationTokenSource = new(TimeSpan.FromSeconds(30));

    public readonly string DefaultSecretName = "password";
    public readonly string DefaultSecretValue = "hunter2";
    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    private KeyVaultSecret? _defaultSecret = null;

    public async ValueTask<SecretClient> GetSecretClientAsync()
    {
        if (_secretClient is not null)
            return _secretClient;

        var options = new SecretClientOptions
        {
            DisableChallengeResourceVerification = true
        };

        var setupModel = await GetClientSetupModelAsync();

        return _secretClient = new SecretClient(setupModel.VaultUri, setupModel.Credential, options);
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

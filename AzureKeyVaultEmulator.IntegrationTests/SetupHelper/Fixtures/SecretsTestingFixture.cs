using Azure.Security.KeyVault.Secrets;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

public class SecretsTestingFixture : KeyVaultClientTestingFixture<SecretClient>
{
    private SecretClient? _secretClient;

    public readonly string DefaultSecretName = "password";
    public readonly string DefaultSecretValue = "hunter2";

    private KeyVaultSecret? _defaultSecret = null;

    public override async ValueTask<SecretClient> GetClientAsync()
    {
        if (_secretClient is not null)
            return _secretClient;

        var options = new SecretClientOptions
        {
            DisableChallengeResourceVerification = true,
            RetryPolicy = _clientRetryPolicy
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

        _secretClient = await GetClientAsync();

        return await _secretClient.SetSecretAsync(secretName, secretValue);
    }
}

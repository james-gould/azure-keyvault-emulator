namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper;

internal sealed class ClientSetupVM(Uri uri, EmulatedTokenCredential cred)
{
    internal Uri VaultUri => uri;
    internal EmulatedTokenCredential Credential => cred;
}

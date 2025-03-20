using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Secrets.Services
{
    public interface IKeyVaultSecretService
    {
        SecretResponse? Get(string name, string version = "");
        SecretResponse? SetSecret(string name, SetSecretModel requestBody);
        DeletedSecretBundle? DeleteSecret(string name, string version = "");
    }
}

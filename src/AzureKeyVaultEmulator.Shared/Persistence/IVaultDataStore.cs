using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Shared.Persistence;

internal interface IVaultDataStore
{
    IEnumerable<SecretBundle> Secrets { get; set; }
}

namespace AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

public interface IPersistedItem : INamedItem
{
    long PrimaryId { get; set; }
}

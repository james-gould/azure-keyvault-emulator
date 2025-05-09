namespace AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

public interface IDeletable
{
    bool Deleted { get; set; }
}

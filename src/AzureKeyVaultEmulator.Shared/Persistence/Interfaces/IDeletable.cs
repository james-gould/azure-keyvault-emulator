namespace AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

/// <summary>
/// Represents a persisted item that may also be deleted, and potentially restored/purged.
/// </summary>
public interface IDeletable
{
    bool Deleted { get; set; }
}

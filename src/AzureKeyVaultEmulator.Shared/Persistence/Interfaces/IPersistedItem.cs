namespace AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

/// <summary>
/// Represents an item that is persisted in the database, but always owned by a top-level parent.
/// </summary>
public interface IPersistedItem
{
    Guid PersistedId { get; set; }
}

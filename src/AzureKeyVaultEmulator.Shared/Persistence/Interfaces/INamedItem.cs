namespace AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

/// <summary>
/// Represents a persisted item that must also be named, allowing for querying through the API.
/// </summary>
public interface INamedItem : IPersistedItem, IDeletable
{
    string PersistedName { get; set; }

    string PersistedVersion { get; set; }
}

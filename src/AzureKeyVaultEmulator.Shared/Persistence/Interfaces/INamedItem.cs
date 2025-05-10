namespace AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

/// <summary>
/// Allows for easier extension method creation when porting from Dictionary to DbSet
/// </summary>
public interface INamedItem : IPersistedItem, IDeletable
{
    string PersistedName { get; set; }

    string PersistedVersion { get; set; }
}

namespace AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

/// <summary>
/// Allows for easier extension method creation when porting from Dictionary to DbSet
/// </summary>
public interface INamedItem
{
    string PersistedName { get; set; }
}

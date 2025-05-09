namespace AzureKeyVaultEmulator.Shared.Persistence;

/// <summary>
/// Allows for easier extension method creation when porting from Dictionary to DbSet
/// </summary>
public interface INamedItem
{
    string Name { get; set; }
}

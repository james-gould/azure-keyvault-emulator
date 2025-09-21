using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AzureKeyVaultEmulator.Shared.Persistence;

/// <summary>
/// Design-time factory for VaultContext to enable Entity Framework migrations.
/// </summary>
public class VaultContextFactory : IDesignTimeDbContextFactory<VaultContext>
{
    public VaultContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VaultContext>();
        
        // Use a temporary SQLite database for design-time operations
        optionsBuilder.UseSqlite("Data Source=:memory:");
        
        return new VaultContext(optionsBuilder.Options);
    }
}
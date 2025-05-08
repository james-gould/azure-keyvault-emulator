using AzureKeyVaultEmulator.Shared.Constants;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using AzureKeyVaultEmulator.Shared.Utilities;
using Microsoft.EntityFrameworkCore;

namespace AzureKeyVaultEmulator.Shared.Persistence;

public sealed class VaultContext : DbContext
{
    public VaultContext() { }

    public DbSet<SecretBundle> Secrets { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var shouldPersist = EnvironmentConstants.UsePersistedDataStore.GetFlag();

        var connectionString = PersistenceUtils.CreateSQLiteConnectionString(shouldPersist);

        optionsBuilder.UseSqlite(connectionString);

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SecretBundle>()
            .HasKey(s => s.SecretIdentifier);

        base.OnModelCreating(modelBuilder);
    }
}

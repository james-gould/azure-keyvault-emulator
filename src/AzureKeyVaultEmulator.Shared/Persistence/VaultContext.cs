using AzureKeyVaultEmulator.Shared.Constants;
using AzureKeyVaultEmulator.Shared.Models.Keys;
//using AzureKeyVaultEmulator.Shared.Models.Secrets;
using AzureKeyVaultEmulator.Shared.Utilities;
using Microsoft.EntityFrameworkCore;

namespace AzureKeyVaultEmulator.Shared.Persistence;

public sealed class VaultContext : DbContext
{
    public VaultContext() { }

    //public DbSet<SecretBundle> Secrets { get; set; }
    public DbSet<KeyBundle> Keys { get; set; }
    public DbSet<JsonWebKeyModel> JsonWebKeys { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var shouldPersist = EnvironmentConstants.UsePersistedDataStore.GetFlag();

        var debugPath = @"C:/Users/James/keyvaultemulator/certs/";

        var connectionString = PersistenceUtils.CreateSQLiteConnectionString(shouldPersist, debugPath);

        optionsBuilder.UseSqlite(connectionString);

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KeyBundle>(e =>
        {
            e.HasKey(x => x.PrimaryId);
        });

        modelBuilder.Entity<JsonWebKeyModel>(e =>
        {
            e.HasKey(x => x.PrimaryId);
        });

        base.OnModelCreating(modelBuilder);
    }
}

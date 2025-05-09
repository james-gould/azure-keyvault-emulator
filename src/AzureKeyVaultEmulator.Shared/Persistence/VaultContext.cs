using AzureKeyVaultEmulator.Shared.Models.Keys;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using Microsoft.EntityFrameworkCore;

namespace AzureKeyVaultEmulator.Shared.Persistence;

public sealed class VaultContext(DbContextOptions<VaultContext> opt) : DbContext(opt)
{
    public DbSet<SecretBundle> Secrets { get; set; }
    public DbSet<KeyBundle> Keys { get; set; }
    public DbSet<JsonWebKeyModel> JsonWebKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KeyBundle>(e =>
        {
            e.HasKey(x => x.PrimaryId);
            e.Navigation(e => e.Key).AutoInclude();
        });

        modelBuilder.Entity<JsonWebKeyModel>(e =>
        {
            e.HasKey(x => x.PrimaryId);
        });

        base.OnModelCreating(modelBuilder);
    }
}

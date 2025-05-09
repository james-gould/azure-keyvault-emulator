using System.Linq.Expressions;
using AzureKeyVaultEmulator.Shared.Models.Keys;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AzureKeyVaultEmulator.Shared.Persistence;

public sealed class VaultContext(DbContextOptions<VaultContext> opt) : DbContext(opt)
{
    public DbSet<SecretBundle> Secrets { get; set; }
    public DbSet<KeyBundle> Keys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KeyBundle>(e =>
        {
            e.HasKey(x => x.PrimaryId);

            e.OwnsNavigation(x => x.Attributes);
            e.OwnsNavigation(x => x.Key);
        });

        modelBuilder.Entity<SecretBundle>(e => e.HasKey(x => x.PrimaryId));

        base.OnModelCreating(modelBuilder);
    }
}

public static class ModelBuilderExtensions
{
    public static void OwnsNavigation<TEntity, TDependent>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TDependent?>> navigation)
        where TEntity : class
        where TDependent : class
    {
        builder.OwnsOne(navigation);
        builder.Navigation(navigation).AutoInclude();
    }
}

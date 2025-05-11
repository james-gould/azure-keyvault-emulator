using System.Linq.Expressions;
using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Keys;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AzureKeyVaultEmulator.Shared.Persistence;

public sealed class VaultContext(DbContextOptions<VaultContext> opt) : DbContext(opt)
{
    public DbSet<SecretBundle> Secrets { get; set; }
    public DbSet<KeyBundle> Keys { get; set; }
    public DbSet<CertificateBundle> Certificates { get; set; }
    public DbSet<CertificatePolicy> CertificatePolicies { get; set; }
    public DbSet<IssuerBundle> Issuers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KeyBundle>(e =>
        {
            e.HasKey(x => x.PrimaryId);

            e.OwnsNavigation(x => x.Attributes);
            e.OwnsNavigation(x => x.Key);
        });

        modelBuilder.Entity<SecretBundle>(e =>
        {
            e.HasKey(x => x.PrimaryId);
            e.OwnsNavigation(x => x.Attributes);
        });

        modelBuilder.Entity<CertificateBundle>(e =>
        {
            e.HasKey(x => x.PrimaryId);

            e.OwnsNavigation(x => x.Attributes);

            e.HasChildNavigation(x => x.CertificatePolicy);
        });

        modelBuilder.Entity<CertificatePolicy>(e =>
        {
            e.HasKey(x => x.PrimaryId);

            e.HasChildNavigation(x => x.Issuer);

            e.OwnsNavigation(x => x.CertificateAttributes);
            e.OwnsNavigation(x => x.KeyProperties);
            e.OwnsNavigation(x => x.SecretProperies);

            e.OwnsOne(x => x.CertificateProperties, props =>
            {
                props.OwnsOne(x => x.SubjectAlternativeNames);
                props.Navigation(x => x.SubjectAlternativeNames).AutoInclude();
            });
            e.Navigation(x => x.CertificateProperties).AutoInclude();
        });

        modelBuilder.Entity<IssuerBundle>(e =>
        {
            e.HasKey(x => x.PrimaryId);

            e.OwnsNavigation(x => x.Attributes);
            e.OwnsNavigation(x => x.Credentials);
            e.OwnsNavigation(x => x.OrganisationDetails);
        });

        base.OnModelCreating(modelBuilder);
    }
}

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Creates the FK relationship and navigation between 2 <see cref="DbSet{TEntity}"/>, where <typeparamref name="TDependent"/> is a child object of <typeparamref name="TEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TDependent"></typeparam>
    /// <param name="builder"></param>
    /// <param name="navigation"></param>
    public static void HasChildNavigation<TEntity, TDependent>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TDependent?>> navigation)
        where TEntity : class
        where TDependent : class, IPersistedItem
    {
        builder.HasOne(navigation).WithOne().HasForeignKey<TDependent>(x => x.PrimaryId);
        builder.Navigation(navigation).AutoInclude();
    }

    /// <summary>
    /// Creates the owned relationship between a <see cref="DbSet{TEntity}"/> of <typeparamref name="TEntity"/> and <typeparamref name="TDependent"/> without a <see cref="DbSet{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TDependent"></typeparam>
    /// <param name="builder"></param>
    /// <param name="navigation"></param>
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

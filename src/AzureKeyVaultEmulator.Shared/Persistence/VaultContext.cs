using System.Linq.Expressions;
using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Keys;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
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
    public DbSet<X509CertificateProperties> X509Properties { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KeyBundle>(e =>
        {
            e.HasKey(x => x.PrimaryId);

            e.OwnsNavigation(x => x.Attributes);
            e.OwnsNavigation(x => x.Key);
        });

        modelBuilder.Entity<SecretBundle>(e => e.HasKey(x => x.PrimaryId));

        modelBuilder.Entity<CertificateBundle>(e =>
        {
            e.HasKey(x => x.PrimaryId);

            e.OwnsNavigation(x => x.Attributes);
        });

        modelBuilder.Entity<IssuerBundle>(e =>
        {
            e.HasKey(x => x.PrimaryId);

            e.OwnsNavigation(x => x.Attributes);
            e.OwnsNavigation(x => x.Credentials);
            e.OwnsNavigation(x => x.OrganisationDetails);
        });

        modelBuilder.Entity<CertificatePolicy>(e =>
        {
            e.HasKey(x => x.PrimaryId);

            e.OwnsNavigation(x => x.CertificateAttributes);
            e.OwnsNavigation(x => x.KeyProperties);
            e.OwnsNavigation(x => x.SecretProperies);
        });

        modelBuilder.Entity<X509CertificateProperties>(e =>
        {
            e.HasKey(x => x.PrimaryId);

            e.OwnsNavigation(x => x.SubjectAlternativeNames);
        });

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

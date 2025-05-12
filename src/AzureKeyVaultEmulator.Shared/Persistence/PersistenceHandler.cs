using AzureKeyVaultEmulator.Shared.Constants;
using AzureKeyVaultEmulator.Shared.Persistence;
using AzureKeyVaultEmulator.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AzureKeyVaultEmulator.ApiConfiguration;

public static class PersistenceHandler
{
    public static IServiceCollection AddVaultPersistenceLayer(this IServiceCollection services)
    {
        var shouldPersist = EnvironmentConstants.UsePersistedDataStore.GetFlag();

        var connectionString = PersistenceUtils.CreateSQLiteConnectionString(shouldPersist);

        services.AddDbContext<VaultContext>(options => options.UseSqlite(connectionString));

        return services;
    }
}

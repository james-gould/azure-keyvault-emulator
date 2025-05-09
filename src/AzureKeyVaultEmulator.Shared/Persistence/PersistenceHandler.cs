using AzureKeyVaultEmulator.Shared.Constants;
using AzureKeyVaultEmulator.Shared.Persistence;
using AzureKeyVaultEmulator.Shared.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AzureKeyVaultEmulator.ApiConfiguration;

public static class PersistenceHandler
{
    public static IServiceCollection AddVaultPersistenceLayer(this IServiceCollection services)
    {
        SqliteConnection connection = null!;

        services.AddDbContext<VaultContext>((sp, options) =>
        {
            var shouldPersist = EnvironmentConstants.UsePersistedDataStore.GetFlag();
            var debugPath = @"C:/Users/James/keyvaultemulator/certs/";

            var connectionString = PersistenceUtils.CreateSQLiteConnectionString(shouldPersist, debugPath);

            if (shouldPersist)
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                connection = new SqliteConnection(connectionString);
                connection.Open();
                options.UseSqlite(connection);
            }
        });

        if(connection != null)
            services.AddSingleton(connection);

        return services;
    }

    private  static DbContextOptions<VaultContext> GetOptions()
    {
        var shouldPersist = EnvironmentConstants.UsePersistedDataStore.GetFlag();
        var debugPath = @"C:/Users/James/keyvaultemulator/certs/";

        var connectionString = PersistenceUtils.CreateSQLiteConnectionString(shouldPersist, debugPath);

        if (shouldPersist)
            return new DbContextOptionsBuilder<VaultContext>().UseSqlite(connectionString).Options;

        var connection = new SqliteConnection(connectionString);
        connection.Open();

        return new DbContextOptionsBuilder<VaultContext>().UseSqlite(connection).Options;
    }
}

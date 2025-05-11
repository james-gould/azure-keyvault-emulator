using System.Text.Json;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

namespace AzureKeyVaultEmulator.Shared.Utilities;

public static class PersistenceUtils
{
    /// <summary>
    /// Constructs the ConnectionString for an SQLite database.
    /// </summary>
    /// <param name="shouldPersist">Flag for using in memory or persisted on disk.</param>
    /// <returns>A fully qualified connection string.</returns>
    public static string CreateSQLiteConnectionString(bool shouldPersist)
    {
        var root = "Data Source=";
        var dbName = shouldPersist ? "emulator" : Guid.NewGuid().Neat();
        var dbDir = shouldPersist ? "certs/" : Path.GetTempPath();

        return $"{root}{dbDir}{dbName}.db";
    }
}

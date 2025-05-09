namespace AzureKeyVaultEmulator.Shared.Utilities;

internal static class PersistenceUtils
{
    /// <summary>
    /// Constructs the ConnectionString for an SQLite database.
    /// </summary>
    /// <param name="shouldPersist">Flag for using in memory or persisted on disk.</param>
    /// <param name="mountedDirName">The directory name mapped onto the host machine.</param>
    /// <returns>A fully qualified connection string.</returns>
    public static string CreateSQLiteConnectionString(bool shouldPersist, string mountedDirName = "certs")
    {
        var root = "Data Source=";
        var dbName = "emulator";

        if (!shouldPersist)
            return $"${root}{dbName}.db;Mode=Memory;Cache=Shared";

        return $"{root}{mountedDirName}/{dbName}.db";
    }
}

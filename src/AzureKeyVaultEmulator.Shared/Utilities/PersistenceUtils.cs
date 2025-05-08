namespace AzureKeyVaultEmulator.Shared.Utilities;

internal static class PersistenceUtils
{
    /// <summary>
    /// Constructs the ConnectionString for an SQLite database.
    /// </summary>
    /// <param name="isInMemoryDatabase">Flag for using in memory or persisted on disk.</param>
    /// <param name="mountedDirName">The directory name mapped onto the host machine.</param>
    /// <returns>A fully qualified connection string.</returns>
    public static string CreateSQLiteConnectionString(bool isInMemoryDatabase, string mountedDirName = "certs")
    {
        var @base = "Data Source=emulator";

        if (isInMemoryDatabase)
            return $"${@base};Mode=Memory;Cache=Shared";

        return $"{mountedDirName}/{@base}.db";
    }
}

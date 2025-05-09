using System.Text.Json;

namespace AzureKeyVaultEmulator.Shared.Utilities;

public static class PersistenceUtils
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
            return $"{root}{dbName};Mode=Memory;Cache=Shared";

        return $"{root}{mountedDirName}/{dbName}.db";
    }

    /// <summary>
    /// Creates a new instance of <typeparamref name="T"/>, allowing the same entity to be inserted twice. Hack at best.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static T Clone<T>(this T obj) where T : notnull
    {
        var json = JsonSerializer.Serialize(obj);

        return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException($"Failed to clone item for persistence layer.");
    }
}

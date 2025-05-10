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

    /// <summary>
    /// Creates a new instance of <typeparamref name="T"/>, allowing the same entity to be inserted twice. Hack at best.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static T Clone<T>(this T obj) where T : notnull, INamedItem
    {
        var json = JsonSerializer.Serialize(obj);

        return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException($"Failed to clone item for persistence layer.");
    }
}

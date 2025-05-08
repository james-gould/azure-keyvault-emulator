namespace AzureKeyVaultEmulator.Shared.Utilities;

internal static class PersistenceUtils
{
    public static string CreateSQLiteConnectionString(bool isInMemoryDatabase, string mountedDirName = "certs")
    {
        var @base = "Data Source=emulator";

        if (isInMemoryDatabase)
            return $"${@base};Mode=Memory;Cache=Shared";

        return $"{mountedDirName}/{@base}.db";
    }
}

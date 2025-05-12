namespace AzureKeyVaultEmulator.Shared.Utilities;

public static class EnvUtils
{
    public static string GetEnvVarOrDefault(this string envVar, string defaultValue)
        => Environment.GetEnvironmentVariable(envVar) ?? defaultValue;

    public static bool GetFlag(this string flagName)
    {
        var fromEnv = Environment.GetEnvironmentVariable(flagName);

        return !string.IsNullOrEmpty(fromEnv) && ConvertTo<bool>(fromEnv);
    }

    private static T ConvertTo<T>(string value)
    {
        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (InvalidCastException)
        {
            throw new InvalidCastException($"Cannot convert {value} to {typeof(T).Name}");
        }
    }
}

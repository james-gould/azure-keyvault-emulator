namespace AzureKeyVaultEmulator.AppHost;

internal static class AppHostExtensions
{
    public static bool GetFlag(this string[] args, string flagName)
    {
        var fromArgs = args.FirstOrDefault(x => x.Contains(flagName, StringComparison.InvariantCultureIgnoreCase));

        return !string.IsNullOrEmpty(fromArgs);
    }
}

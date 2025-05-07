namespace AzureKeyVaultEmulator.Shared.Utilities
{
    public static class CacheUtils
    {
        public static string GetCacheId(this string name, string version = "")
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return $"{name}{version}";
        }
    }
}

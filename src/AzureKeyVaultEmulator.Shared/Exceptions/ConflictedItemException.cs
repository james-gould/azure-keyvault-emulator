namespace AzureKeyVaultEmulator.Shared.Exceptions
{
    public sealed class ConflictedItemException(string name) : Exception($"Conflicted item {name} found in vault.")
    {
        public string Name { get; } = name;
    }
}

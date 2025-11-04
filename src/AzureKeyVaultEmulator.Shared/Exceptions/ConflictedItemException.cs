namespace AzureKeyVaultEmulator.Shared.Exceptions
{
    public sealed class ConflictedItemException(string itemType, string name) : Exception($"Conflicted {itemType} {name} found in vault.")
    {
        public string Name { get; } = name;

        public string ItemType { get; } = itemType;
    }
}

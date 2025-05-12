namespace AzureKeyVaultEmulator.Shared.Exceptions;

public sealed class MissingItemException(string name) : Exception($"Could not find {name} in vault.");

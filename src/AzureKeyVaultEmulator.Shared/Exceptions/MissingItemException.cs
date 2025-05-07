namespace AzureKeyVaultEmulator.Shared.Exceptions;

public sealed class MissingItemException(string msg) : Exception(msg);

namespace AzureKeyVaultEmulator.Shared.Exceptions;

public sealed class SecretDisabledException(string name) : Exception($"Operation get is not allowed on a disabled secret: {name}.");

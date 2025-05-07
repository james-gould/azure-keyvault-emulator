namespace AzureKeyVaultEmulator.Shared.Constants;

public sealed class OperationConstants
{
    // https://github.com/Azure/azure-sdk-for-net/blob/48f989c17416fd9a5620c74a45823bd3668b88c1/sdk/keyvault/Azure.Security.KeyVault.Certificates/src/CertificateOperation.cs#L16-L17
    // "pending" denoted in CertificateClient docs, but not present in SDK client
    public const string Pending = "pending";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";

    public const string CompletedReason = $"All operations are immediately completed in the {AuthConstants.EmulatorName}";
}

namespace AzureKeyVaultEmulator.Shared.Models
{
    public sealed class DeletionRecoveryLevel
    {
        public const string Purgeable = "Purgeable";
        public const string RecoverablePurgeable = "Recoverable+Purgeable";
        public const string Recoverable = "Recoverable";
    }
}

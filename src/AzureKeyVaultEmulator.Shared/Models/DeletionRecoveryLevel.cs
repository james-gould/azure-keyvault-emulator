namespace AzureKeyVaultEmulator.Shared.Models
{
    public static class DeletionRecoveryLevel
    {
        public const string Purgeable = "Purgeable";
        public const string RecoverablePurgeable = "Recoverable+Purgeable";
        public const string Recoverable = "Recoverable";
        public const string RecoverableProtectedSubscription = "Recoverable+ProtectedSubscription";
        public const string CustomizedRecoverablePurgeable = "CustomizedRecoverable+Purgeable";
        public const string CustomizedRecoverable = "CustomizedRecoverable";
        public const string CustomizedRecoverableProtectedSubscription = "CustomizedRecoverable+ProtectedSubscription";
    }
}

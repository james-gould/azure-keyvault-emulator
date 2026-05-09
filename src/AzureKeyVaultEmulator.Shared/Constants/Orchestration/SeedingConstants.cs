namespace AzureKeyVaultEmulator.Shared.Constants.Orchestration;

/// <summary>
/// Shared constants describing the resources seeded by the AppHost when the
/// <see cref="AspireConstants.SeedingTest"/> flag is supplied. Used by both the
/// AppHost itself and the integration tests that verify the seeded values.
/// </summary>
public static class SeedingConstants
{
    public const string SeededSecretName = "first";
    public const string SeededSecretValue = "secretValue";

    public const string SeededCertificateName = "testingCert";

    public const string SeededKeyName = "testingKey";
}

namespace AzureKeyVaultEmulator.Shared.Constants.Orchestration;

public sealed class AspireConstants
{
    public const string EmulatorServiceName = "keyVaultEmulatorApi";
    public const string DebugHelper = "sampleApi";
    public const string Wiremock = "wiremock";

    public const string IntegrationTest = "integration";
    public const string SeedingTest = "seeding";

    /// <summary>
    /// Flag used to boot the AppHost with the emulator running as a container exposed on a fixed
    /// host port (see <see cref="StaticPortTestPort"/>) so the static-port behaviour can be tested.
    /// </summary>
    public const string StaticPortTest = "staticport";

    /// <summary>
    /// The fixed host port the emulator container is exposed on during <see cref="StaticPortTest"/> runs.
    /// Keeping it constant lets a subsequent run reach data (and its embedded vault URIs) created by a prior run.
    /// </summary>
    public const int StaticPortTestPort = 44997;
}

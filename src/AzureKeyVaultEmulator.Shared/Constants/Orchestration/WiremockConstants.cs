namespace AzureKeyVaultEmulator.Shared.Constants.Orchestration;

public sealed class WiremockConstants
{
    public const string EndpointName = "ensureSsl";
    public const string EndpointPath = $"/{EndpointName}";

    public const string HttpClientName = "wiremockClient";

    public const string ConnectivityResponse = "SSL Achieved";
}

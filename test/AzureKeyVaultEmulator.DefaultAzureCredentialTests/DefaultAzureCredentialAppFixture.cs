using System.Net.Http.Json;

namespace AzureKeyVaultEmulator.DefaultAzureCredentialTests;

/// <summary>
/// Spins up the full Aspire AppHost (emulator + Debug Web API authenticating with
/// <c>DefaultAzureCredential</c>) once per xUnit collection so individual tests can
/// share the launched processes.
/// </summary>
public sealed class DefaultAzureCredentialAppFixture : IAsyncLifetime
{
    private static readonly TimeSpan _waitPeriod = TimeSpan.FromMinutes(2);

    private const string _emulatorResource = "keyvault-emulator";
    private const string _debugApiResource = "debug-api";

    private DistributedApplication? _app;

    public HttpClient DebugApi { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.DefaultAzureCredential_AppHost>([], (opts, _) => opts.DisableDashboard = true);

        _app = await builder.BuildAsync();
        await _app.StartAsync();

        var notifications = _app.Services.GetRequiredService<ResourceNotificationService>();

        // The debug API depends on the emulator being healthy first; wait for both.
        await notifications.WaitForResourceHealthyAsync(_emulatorResource).WaitAsync(_waitPeriod);
        await notifications.WaitForResourceHealthyAsync(_debugApiResource).WaitAsync(_waitPeriod);

        DebugApi = _app.CreateHttpClient(_debugApiResource);
    }

    public async Task DisposeAsync()
    {
        DebugApi?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync().ConfigureAwait(false);
    }
}

[CollectionDefinition(nameof(DefaultAzureCredentialAppCollection))]
public sealed class DefaultAzureCredentialAppCollection : ICollectionFixture<DefaultAzureCredentialAppFixture>
{
}

internal static class HttpResponseExtensions
{
    public static async Task<T> ReadJsonAsync<T>(this HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var value = await response.Content.ReadFromJsonAsync<T>();
        Assert.NotNull(value);
        return value!;
    }
}

internal sealed record SecretPayload(string Name, string Value);
internal sealed record KeyPayload(string Name, string Kid);
internal sealed record CertificatePayload(string Name);

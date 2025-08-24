using AzureKeyVaultEmulator.Shared.Constants.Orchestration;
using AzureKeyVaultEmulator.Wiremock.IntegrationTests.Fixtures;

namespace AzureKeyVaultEmulator.Wiremock.IntegrationTests;

public class WiremockTests(WiremockFixture fixture) : IClassFixture<WiremockFixture>
{
    private static TimeSpan _timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task WireMockEndpointReturnsCorrectly()
    {
        var tokenSource = new CancellationTokenSource(_timeout);

        var httpClient = await fixture.GetHttpClient(AspireConstants.DebugHelper);

        var response = await httpClient.GetAsync(WiremockConstants.EndpointName, tokenSource.Token);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(WiremockConstants.ConnectivityResponse, content);
    }

    [Fact]
    public async Task CertificateCreationEndpointEstablishesSSLCorrectly()
    {
        var tokenSource = new CancellationTokenSource(_timeout);

        var httpClient = await fixture.GetHttpClient(AspireConstants.DebugHelper);

        var response = await httpClient.GetAsync("/", tokenSource.Token);

        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Response.Content is a randomly generated GUID, assigned to a newly created Azure Key Vault Emulator certificate
        Assert.True(Guid.TryParse(content, out _));
    }
}

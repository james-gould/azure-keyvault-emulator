using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;
using AzureKeyVaultEmulator.Shared.Constants;

namespace AzureKeyVaultEmulator.IntegrationTests.Emulator;

public class AuthenticationTests(EmulatorTestingFixture fixture) : IClassFixture<EmulatorTestingFixture>
{
    [Fact]
    public async Task ChallengeResponseReturnsCorrectWwwAuthenticateHeader()
    {
        var client = await fixture.GetClientAsync();

        var response = await client.GetAsync("/secrets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var wwwAuth = response.Headers.WwwAuthenticate.ToString();

        Assert.Contains("Bearer", wwwAuth);
        Assert.Contains("authorization=", wwwAuth);
        Assert.Contains("scope=", wwwAuth);
        Assert.Contains("resource=\"https://vault.azure.net\"", wwwAuth);
    }

    [Fact]
    public async Task DefaultChallengeAuthorizationContainsEmulatorUriWithPath()
    {
        var client = await fixture.GetClientAsync();

        var response = await client.GetAsync("/secrets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var wwwAuth = response.Headers.WwwAuthenticate.ToString();

        // Without TENANT_ID, authorization should use EmulatorUri + request path
        Assert.Contains($"authorization=\"{AuthConstants.EmulatorUri}/secrets\"", wwwAuth);
    }

    [Fact]
    public async Task ChallengeResponseScopeContainsDefaultSuffix()
    {
        var client = await fixture.GetClientAsync();

        var response = await client.GetAsync("/secrets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var wwwAuth = response.Headers.WwwAuthenticate.ToString();

        Assert.Contains("/.default\"", wwwAuth);
    }
}

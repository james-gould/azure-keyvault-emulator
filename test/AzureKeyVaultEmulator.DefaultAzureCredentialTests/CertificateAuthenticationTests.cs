namespace AzureKeyVaultEmulator.DefaultAzureCredentialTests;

/// <summary>
/// Certificate-focused tests proving the Debug Web API can create and read certificates via
/// <see cref="Azure.Identity.DefaultAzureCredential"/> against the emulator.
/// </summary>
[Collection(nameof(DefaultAzureCredentialAppCollection))]
public sealed class CertificateAuthenticationTests(DefaultAzureCredentialAppFixture fixture)
{
    [Fact]
    public async Task CreateCertificateViaDefaultAzureCredentialReturnsCertificateTest()
    {
        var name = fixture.FreshlyGeneratedGuid;

        var response = await fixture.DebugApi.PostAsync($"/certificates/{name}", content: null);
        var payload = await response.ReadJsonAsync<CertificatePayload>();

        Assert.Equal(name, payload.Name);
    }

    [Fact]
    public async Task GetCertificateViaDefaultAzureCredentialReturnsCorrectCertificateTest()
    {
        var name = $"cert-{Guid.NewGuid():N}";

        var create = await fixture.DebugApi.PostAsync($"/certificates/{name}", content: null);
        create.EnsureSuccessStatusCode();

        var response = await fixture.DebugApi.GetAsync($"/certificates/{name}");
        var payload = await response.ReadJsonAsync<CertificatePayload>();

        Assert.Equal(name, payload.Name);
    }
}

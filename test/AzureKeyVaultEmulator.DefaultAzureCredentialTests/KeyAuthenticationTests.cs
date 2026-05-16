namespace AzureKeyVaultEmulator.DefaultAzureCredentialTests;

/// <summary>
/// Key-focused tests proving the Debug Web API can create and read keys via
/// <see cref="Azure.Identity.DefaultAzureCredential"/> against the emulator.
/// </summary>
[Collection(nameof(DefaultAzureCredentialAppCollection))]
public sealed class KeyAuthenticationTests(DefaultAzureCredentialAppFixture fixture)
{
    [Fact]
    public async Task CreateKeyViaDefaultAzureCredentialReturnsKeyTest()
    {
        var name = fixture.FreshlyGeneratedGuid;

        var response = await fixture.DebugApi.PostAsync($"/keys/{name}", content: null);
        var payload = await response.ReadJsonAsync<KeyPayload>();

        Assert.Equal(name, payload.Name);
        Assert.False(string.IsNullOrEmpty(payload.Kid));
    }

    [Fact]
    public async Task GetKeyViaDefaultAzureCredentialReturnsCorrectKeyTest()
    {
        var name = fixture.FreshlyGeneratedGuid;

        var create = await fixture.DebugApi.PostAsync($"/keys/{name}", content: null);
        create.EnsureSuccessStatusCode();

        var response = await fixture.DebugApi.GetAsync($"/keys/{name}");
        var payload = await response.ReadJsonAsync<KeyPayload>();

        Assert.Equal(name, payload.Name);
    }
}

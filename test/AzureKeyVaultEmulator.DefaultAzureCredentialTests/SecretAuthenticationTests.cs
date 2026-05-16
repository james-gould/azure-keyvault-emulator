namespace AzureKeyVaultEmulator.DefaultAzureCredentialTests;

/// <summary>
/// Secret-focused tests that prove the Debug Web API (which uses ONLY the official Azure SDK
/// and authenticates with <see cref="Azure.Identity.DefaultAzureCredential"/>) can successfully
/// round-trip a secret through the Azure Key Vault Emulator.
/// </summary>
[Collection(nameof(DefaultAzureCredentialAppCollection))]
public sealed class SecretAuthenticationTests(DefaultAzureCredentialAppFixture fixture)
{
    [Fact]
    public async Task DefaultAzureCredential_Can_Create_A_Secret()
    {
        var name = fixture.FreshlyGeneratedGuid;
        var value = fixture.FreshlyGeneratedGuid;

        var response = await fixture.DebugApi.PostAsync($"/secrets/{name}?value={value}", content: null);

        var payload = await response.ReadJsonAsync<SecretPayload>();

        Assert.Equal(name, payload.Name);
        Assert.Equal(value, payload.Value);
    }

    [Fact]
    public async Task DefaultAzureCredential_Can_Read_A_Secret()
    {
        var name = fixture.FreshlyGeneratedGuid;
        var value = fixture.FreshlyGeneratedGuid;

        var create = await fixture.DebugApi.PostAsync($"/secrets/{name}?value={value}", content: null);
        create.EnsureSuccessStatusCode();

        var get = await fixture.DebugApi.GetAsync($"/secrets/{name}");
        var payload = await get.ReadJsonAsync<SecretPayload>();

        Assert.Equal(value, payload.Value);
    }
}

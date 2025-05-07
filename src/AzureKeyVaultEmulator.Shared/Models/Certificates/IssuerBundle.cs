using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Constants;
using AzureKeyVaultEmulator.Shared.Utilities;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

// https://learn.microsoft.com/en-us/rest/api/keyvault/certificates/get-certificate-issuer/get-certificate-issuer?view=rest-keyvault-certificates-7.4&tabs=HTTP#examples
public sealed class IssuerBundle
{
    [JsonPropertyName("id")]
    public string Identifier => $"{AuthConstants.EmulatorUri}/certificates/issuers/{IssuerName}";

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string IssuerName { get; set; } = string.Empty;

    [JsonPropertyName("credentials")]
    public IssuerCredentials Credentials { get; set; } = new();

    [JsonPropertyName("attributes")]
    public IssuerAttributes Attributes { get; set; } = new();

    [JsonPropertyName("org_details")]
    public OrganisationDetails OrganisationDetails { get; set; } = new();
}

public sealed class IssuerAttributes : AttributeBase;

public sealed class IssuerCredentials
{
    [JsonPropertyName("account_id")]
    public string AccountId { get; set; } = string.Empty;

    [JsonPropertyName("pwd")]
    public string Password { get; set; } = string.Empty;
}

public sealed class OrganisationDetails
{
    [JsonPropertyName("id")]
    public string Identifier { get; set; } = Guid.NewGuid().Neat();

    [JsonPropertyName("admin_details")]
    public IEnumerable<AdministratorDetails> AdministratorDetails { get; set; } = [];
}

public sealed class AdministratorDetails
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;
}

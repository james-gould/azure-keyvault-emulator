using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Constants;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;
using AzureKeyVaultEmulator.Shared.Utilities;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

// https://learn.microsoft.com/en-us/rest/api/keyvault/certificates/get-certificate-issuer/get-certificate-issuer?view=rest-keyvault-certificates-7.4&tabs=HTTP#examples
public sealed class IssuerBundle : INamedItem
{
    [Key]
    [JsonIgnore]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid PersistedId { get; set; } = Guid.NewGuid();

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string PersistedName { get; set; } = Guid.NewGuid().Neat();

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string PersistedVersion { get; set; } = Guid.NewGuid().Neat();

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public bool Deleted { get; set; } = false;

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
    public OrganisationDetails? OrganisationDetails { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public IList<CertificatePolicy> Policies { get; set; } = [];
}

public sealed class IssuerAttributes : AttributeBase;

public sealed class IssuerCredentials : IPersistedItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Guid PersistedId { get; set; } = Guid.NewGuid();

    [JsonPropertyName("account_id")]
    public string AccountId { get; set; } = string.Empty;

    [JsonPropertyName("pwd")]
    public string Password { get; set; } = string.Empty;
}

public sealed class OrganisationDetails : IPersistedItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Guid PersistedId { get; set; } = Guid.NewGuid();

    [JsonPropertyName("id")]
    public string Identifier { get; set; } = Guid.NewGuid().Neat();

    public string BackingAdminDetails { get; set; } = "[]";

    [JsonPropertyName("admin_details")]
    [NotMapped]
    public IEnumerable<AdministratorDetails> AdministratorDetails
    {
        get => JsonSerializer.Deserialize<IEnumerable<AdministratorDetails>>(BackingAdminDetails) ?? [];
        set => BackingAdminDetails = JsonSerializer.Serialize(value);
    }
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

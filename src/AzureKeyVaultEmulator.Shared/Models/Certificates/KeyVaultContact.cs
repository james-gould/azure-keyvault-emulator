using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

/// <summary>
/// <para>Although technically for certificates, the actual use case of Contacts is at a key vault level.</para>
/// <para>They're only referenced in the Certificates API documentation, ideally this would be a shared class but keeping it domain specific follows the existing pattern.</para>
/// <para>Note: this is a different entity to <see cref="AdministratorDetails"/>, although they're very similar.</para>
/// <para>Reference documentation: https://learn.microsoft.com/en-us/rest/api/keyvault/certificates/set-certificate-contacts/set-certificate-contacts</para>
/// </summary>
public sealed class KeyVaultContact
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;
}

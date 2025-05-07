using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Constants;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;

public sealed class CertificateContacts 
{
    [JsonPropertyName("id")]
    public string Id { get; } = $"{AuthConstants.EmulatorUri}/certificates/contacts";

    [JsonPropertyName("contacts")]
    public IEnumerable<KeyVaultContact> Contacts { get; set; } = [];
}

using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;

public sealed class SetContactsRequest
{
    [JsonPropertyName("contacts")]
    public List<KeyVaultContact> Contacts { get; set; } = [];
}

using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;

public sealed class SetContactsRequest
{
    [JsonPropertyName("contacts")]
    public IEnumerable<KeyVaultContact> Contacts { get; set; } = [];
}

using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Constants;
using AzureKeyVaultEmulator.Shared.Utilities;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

// https://learn.microsoft.com/en-us/rest/api/keyvault/certificates/create-certificate/create-certificate?view=rest-keyvault-certificates-7.4&tabs=HTTP#certificateoperation
public sealed class CertificateOperation(string id, string certName)
{
    [JsonPropertyName("cancellation_requested")]
    public bool CancellationRequested { get; set; } = false;

    [JsonPropertyName("csr")]
    public string CertificateSigningRequest { get; set; } = ""; // Can't see a way to pass a signing cert/csr?

    //[JsonPropertyName("error")]
    //public KeyVaultError? Error { get; set; } = new(); // Might be worth piping this in if something does go wrong?

    [JsonPropertyName("id")]
    public string CertificateIdentifier { get; set; } = id;

    [JsonPropertyName("issuer")]
    public IssuerParameters IssuerParameters { get; set; } = new();

    [JsonPropertyName("request_id")]
    public string RequestId { get; set; } = Guid.NewGuid().Neat();

    [JsonPropertyName("status")]
    public string Status { get; set; } = OperationConstants.Completed;

    [JsonPropertyName("status_details")]
    public string StatusDetails { get; set; } = OperationConstants.CompletedReason;

    [JsonPropertyName("target")]
    public string Target { get; set; } = $"/certificates/{certName}";
}

public sealed class IssuerParameters
{
    [JsonPropertyName("cert_transparency")]
    public bool Transparency { get; set;} = true;

    [JsonPropertyName("cty")]
    public string CertificateType { get; set; } = ""; // Optional, not concerned about it yet.

    [JsonPropertyName("name")]
    public string Name { get; set; } = "AzureKeyVaultEmulator-SelfSigned";

}

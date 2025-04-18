using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Constants;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

// https://learn.microsoft.com/en-us/rest/api/keyvault/certificates/create-certificate/create-certificate?view=rest-keyvault-certificates-7.4&tabs=HTTP#certificateoperation
public sealed class CertificateOperation(string id, string certName)
{
    [JsonPropertyName("cancellation_requested")]
    public const bool CancellationRequested = false;

    [JsonPropertyName("csr")]
    public const string CertificateSigningRequest = ""; // Can't see a way to pass a signing cert/csr?

    [JsonPropertyName("error")]
    public static KeyVaultError? Error => null; // Might be worth piping this in if something does go wrong?

    [JsonPropertyName("id")]
    public string CertificateIdentifier => id;

    [JsonPropertyName("issuer")]
    public IssuerParameters IssuerParameters { get; set; } = new();

    [JsonPropertyName("request_id")]
    public static string RequestId => Guid.NewGuid().ToString("n");

    [JsonPropertyName("status")]
    public static string Status => OperationConstants.Completed;

    [JsonPropertyName("status_details")]
    public static string StatusDetails => OperationConstants.CompletedReason;

    [JsonPropertyName("target")]
    public string Target = $"/certificates/{certName}";
}

public sealed class IssuerParameters
{
    [JsonPropertyName("cert_transparency")]
    public const bool Transparency = true;

    [JsonPropertyName("cty")]
    public const string CertificateType = ""; // Optional, not concerned about it yet.

    [JsonPropertyName("name")]
    public const string Name = "AzureKeyVaultEmulator-SelfSigned";

}

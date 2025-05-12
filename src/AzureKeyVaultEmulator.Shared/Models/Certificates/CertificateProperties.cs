using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Constants;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

/// <summary>
/// <para>Encapsulates metadata about the certificate.</para>
/// <para>Not listed on the API, very cool Azure, but throws <see cref="InvalidOperationException"/> if required items are missing.</para>
/// </summary>
public class CertificateProperties : TaggedModel
{
    [JsonPropertyName("id")]
    public required string CertificateIdentifier { get; set; }

    [JsonPropertyName("recoveryId")]
    public string RecoveryId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public required string CertificateName { get; set; }

    [JsonPropertyName("vaultUri")]
    public required Uri VaultUri { get; set; }

    [JsonPropertyName("x5t")]
    public required string X509Thumbprint { get; set; }

    [JsonPropertyName("status")]
    public static string OperationStatus = OperationConstants.Completed; // del

    // Recovery not currently supported. Raise an issue if it's required please.
    [JsonPropertyName("recoveryLevelDays")]
    public int RecoveryLevelDays => 0;

    [JsonPropertyName("attributes")]
    public CertificateAttributesModel Attributes { get; set; } = new();
}

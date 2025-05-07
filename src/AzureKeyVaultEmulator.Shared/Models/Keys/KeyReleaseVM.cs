using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Constants;

namespace AzureKeyVaultEmulator.Shared.Models.Keys;

public sealed class KeyReleaseVM(string aas)
{
    [JsonPropertyName("aas-ehd")]
    public string AasEhd { get; set; } = aas;

    [JsonPropertyName("iss")]
    public static string Issuer => $"{AuthConstants.EmulatorUri}/keys";

    [JsonPropertyName("sgx-mrenclave")]
    public static string SGXEnclave => "0000000000000000000000000000000000000000000000000000000000000000";

    [JsonPropertyName("sgx-mrsigner")]
    public static string SGXMrsigner => "86788fe40448f2a12e20bf8d5e7a1c3139bc5fdc1432b370c1da3489ab649a85";

    [JsonPropertyName("is-debuggable")]
    public static bool Debuggable => true;

    [JsonPropertyName("tee")]
    public static string Tee => "sgx";

    [JsonPropertyName("iat")]
    public static long IssuedAt => DateTimeOffset.Now.ToUnixTimeSeconds();

    [JsonPropertyName("exp")]
    public static long Expiry => DateTimeOffset.Now.AddDays(31).ToUnixTimeSeconds();

}

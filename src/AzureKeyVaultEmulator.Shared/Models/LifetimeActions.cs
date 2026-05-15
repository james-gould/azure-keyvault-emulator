using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models;
public sealed class LifetimeActions
{
    [JsonPropertyName("trigger")]
    public TriggerAction? TriggerAction { get; set; }

    [JsonPropertyName("action")]
    public ActionType? Action { get; set; }
}

public sealed class TriggerAction
{
    [JsonPropertyName("timeAfterCreate")]
    public string TimeAfterCreate { get; set; } = string.Empty;

    [JsonPropertyName("days_before_expiry")]
    public int? DaysBeforeExpiry { get; set; } = null;
}

public sealed class ActionType
{
    [JsonPropertyName("action_type")]
    public string Type { get; set; } = string.Empty;
}


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
}

public sealed class ActionType
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}


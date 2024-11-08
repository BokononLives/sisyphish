using System.Text.Json.Serialization;

namespace sisyphish.Discord.Models;

public class DiscordComponent
{
    [JsonPropertyName("type")] public DiscordMessageComponentType Type { get; set; }
    [JsonPropertyName("custom_id")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? CustomId { get; set; }
    [JsonPropertyName("label")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Label { get; set; }
    [JsonPropertyName("style")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public DiscordButtonStyleType? Style { get; set; }
    [JsonPropertyName("components")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public List<DiscordComponent> Components { get; set; } = [];
}
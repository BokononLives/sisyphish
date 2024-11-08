using System.Text.Json.Serialization;

namespace sisyphish.Discord.Models;

public class DiscordInteractionEdit
{
    [JsonPropertyName("content")] public string? Content { get; set; }
    [JsonPropertyName("components")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public List<DiscordComponent> Components { get; set; } = [];
}
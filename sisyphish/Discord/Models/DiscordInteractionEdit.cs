using System.Text.Json.Serialization;

namespace sisyphish.Discord.Models;

public class DiscordInteractionEdit
{
    [JsonPropertyName("content")] public string? Content { get; set; }
}
using System.Text.Json.Serialization;

namespace sisyphish.Tools.Discord.Core.Models;

public class DiscordInteractionEdit
{
    [JsonPropertyName("content")] public string? Content { get; set; }
}
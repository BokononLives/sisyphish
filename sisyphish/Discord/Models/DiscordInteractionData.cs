using System.Text.Json.Serialization;

namespace sisyphish.Discord.Models;

public class DiscordInteractionData
{
    [JsonConverter(typeof(JsonStringEnumConverter))] public DiscordCommandName? Name { get; set; }
    public string? CustomId { get; set; }
}
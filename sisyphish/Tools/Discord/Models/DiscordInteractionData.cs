using System.Text.Json.Serialization;

namespace sisyphish.Tools.Discord.Models;

public class DiscordInteractionData
{
    [JsonConverter(typeof(JsonStringEnumConverter))] public DiscordCommandName? Name { get; set; }
}
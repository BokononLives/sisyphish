using System.Text.Json.Serialization;
using sisyphish.Tools.Discord.Sisyphish.Models;

namespace sisyphish.Tools.Discord.Core.Models;

public class DiscordInteractionData
{
    [JsonConverter(typeof(JsonStringEnumConverter))] public DiscordCommandName? Name { get; set; }
}
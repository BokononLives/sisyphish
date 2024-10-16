using System.Text.Json.Serialization;

namespace sisyphish.Tools.Discord.Models;

public interface IDiscordInteractionResponse
{
    [JsonIgnore] DiscordInteractionResponseType ResponseType { get; }
}
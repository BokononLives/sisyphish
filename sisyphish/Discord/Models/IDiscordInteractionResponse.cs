using System.Text.Json.Serialization;

namespace sisyphish.Discord.Models;

public interface IDiscordInteractionResponse
{
    [JsonIgnore] DiscordInteractionResponseType ResponseType { get; }
}

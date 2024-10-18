using System.Text.Json.Serialization;

namespace sisyphish.Tools.Discord.Core.Models;

public interface IDiscordInteractionResponse
{
    [JsonIgnore] DiscordInteractionResponseType ResponseType { get; }
}
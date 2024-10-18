using System.Text.Json.Serialization;

namespace sisyphish.Tools.Discord.Core.Models;

public class DiscordInteractionErrorResponse : IDiscordInteractionResponse
{
    [JsonIgnore] public DiscordInteractionResponseType ResponseType => DiscordInteractionResponseType.DiscordInteractionErrorResponse;
    public string? Error { get; set; }
}
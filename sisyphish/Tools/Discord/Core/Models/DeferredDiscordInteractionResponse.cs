using System.Text.Json.Serialization;

namespace sisyphish.Tools.Discord.Core.Models;

public class DeferredDiscordInteractionResponse : IDiscordInteractionResponse
{
    [JsonIgnore] public DiscordInteractionResponseType ResponseType => DiscordInteractionResponseType.DeferredDiscordInteractionResponse;
    [JsonPropertyName("type")] public DiscordInteractionResponseContentType ContentType => DiscordInteractionResponseContentType.DeferredChannelMessageWithSource;
}
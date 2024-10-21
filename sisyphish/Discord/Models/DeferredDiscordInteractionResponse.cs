using System.Text.Json.Serialization;

namespace sisyphish.Discord.Models;

public class DeferredDiscordInteractionResponse : IDiscordInteractionResponse
{
    [JsonIgnore] public DiscordInteractionResponseType ResponseType => DiscordInteractionResponseType.DeferredDiscordInteractionResponse;
    [JsonPropertyName("type")] public DiscordInteractionResponseContentType ContentType => DiscordInteractionResponseContentType.DeferredChannelMessageWithSource;
}
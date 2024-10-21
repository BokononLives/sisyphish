using System.Text.Json.Serialization;

namespace sisyphish.Discord.Models;

public class DiscordInteractionResponse : IDiscordInteractionResponse
{
    [JsonIgnore] public DiscordInteractionResponseType ResponseType => DiscordInteractionResponseType.DiscordInteractionResponse;
    [JsonPropertyName("type")] public DiscordInteractionResponseContentType ContentType { get; set; }
    public DiscordInteractionResponseData? Data { get; set; }
}
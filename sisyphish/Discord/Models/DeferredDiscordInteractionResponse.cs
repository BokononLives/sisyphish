using System.Text.Json.Serialization;

namespace sisyphish.Discord.Models;

public class DeferredDiscordInteractionResponse : IDiscordInteractionResponse
{
    [JsonIgnore] public DiscordInteractionResponseType ResponseType => DiscordInteractionResponseType.DeferredDiscordInteractionResponse;
    [JsonIgnore] public bool IsEphemeral { get; set; }
    public DiscordInteractionResponseData? Data
    {
        get
        {
            return IsEphemeral
                ? null
                : new DiscordInteractionResponseData
                {
                    Flags = [DiscordInteractionResponseFlags.Ephemeral]
                };
        }
    }
}
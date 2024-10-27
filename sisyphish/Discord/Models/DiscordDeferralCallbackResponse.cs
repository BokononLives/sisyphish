using System.Text.Json.Serialization;

namespace sisyphish.Discord.Models;

public class DiscordDeferralCallbackResponse
{
    [JsonPropertyName("type")] public int Type => (int)DiscordInteractionResponseContentType.DeferredChannelMessageWithSource;
}
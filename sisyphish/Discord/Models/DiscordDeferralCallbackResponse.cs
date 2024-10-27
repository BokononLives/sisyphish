using System.Text.Json.Serialization;

namespace sisyphish.Discord.Models;

public class DiscordDeferralCallbackResponse
{
    [JsonPropertyName("type")] public static int Type => (int)DiscordInteractionResponseContentType.DeferredChannelMessageWithSource;
}
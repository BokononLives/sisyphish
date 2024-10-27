using System.Text.Json.Serialization;

namespace sisyphish.Discord.Models;

public class DiscordDeferralCallbackResponse
{
    [JsonPropertyName("type")] public const int Type = (int)DiscordInteractionResponseContentType.DeferredChannelMessageWithSource;
}
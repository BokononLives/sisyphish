using System.Text.Json.Serialization;

namespace sisyphish.Discord.Models;

public class DiscordDeferralCallbackResponse
{
    public int Type => (int)DiscordInteractionResponseContentType.DeferredChannelMessageWithSource;
    [JsonIgnore] public bool IsEphemeral { get; set; }
    public DiscordInteractionResponseData? Data
    {
        get
        {
            return IsEphemeral
                ? new DiscordInteractionResponseData
                {
                    Flags = DiscordInteractionResponseFlags.Ephemeral
                }
                : null;
        }
    }
}
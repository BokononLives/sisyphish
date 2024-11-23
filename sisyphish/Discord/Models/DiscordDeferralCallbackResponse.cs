namespace sisyphish.Discord.Models;

public class DiscordDeferralCallbackResponse
{
    public int Type => (int)DiscordInteractionResponseContentType.DeferredChannelMessageWithSource;
}
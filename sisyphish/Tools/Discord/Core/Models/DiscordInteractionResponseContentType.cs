namespace sisyphish.Tools.Discord.Core.Models;

public enum DiscordInteractionResponseContentType
{
    Pong = 1,
    ChannelMessageWithSource = 4,
    DeferredChannelMessageWithSource = 5,
    DeferredUpdateMessage = 6,
    UpdateMessage = 7,
    ApplicationCommandAutocompleteResult = 8,
    Modal = 9,
    PremiumRequired = 10
}
using sisyphish.Tools.Discord.Models;

namespace sisyphish.Tools.Discord;

public interface IDiscordInteractionProcessor
{
    Task<IDiscordInteractionResponse> ProcessDiscordInteraction(DiscordInteraction interaction);
}
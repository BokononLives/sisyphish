using sisyphish.Tools.Discord.Core.Models;

namespace sisyphish.Tools.Discord;

public interface IDiscordInteractionProcessor
{
    Task<IDiscordInteractionResponse> ProcessDiscordInteraction(DiscordInteraction interaction);
}
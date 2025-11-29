using sisyphish.Discord.Models;

namespace sisyphish.Sisyphish.Processors;

public interface ICommandProcessor
{
    DiscordCommandName Command { get; }
    Task<IDiscordInteractionResponse> ProcessInitialCommand(DiscordInteraction interaction);
    Task ProcessFollowUpToCommand(DiscordInteraction interaction);
}

using sisyphish.Discord.Models;

namespace sisyphish.Discord;

public interface IDiscordService
{
    Task DeferResponse(DiscordInteraction interaction);
    Task EditResponse(DiscordInteraction interaction, string content, List<DiscordComponent> components);
}

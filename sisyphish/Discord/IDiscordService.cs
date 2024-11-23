using sisyphish.Discord.Models;

namespace sisyphish.Discord;

public interface IDiscordService
{
    Task DeferResponse(DiscordInteraction interaction, bool isEphemeral);
    Task EditResponse(DiscordInteraction interaction, string content, List<DiscordComponent> components);
    Task SendFollowupResponse(DiscordInteraction interaction, string content, List<DiscordComponent> components);
}

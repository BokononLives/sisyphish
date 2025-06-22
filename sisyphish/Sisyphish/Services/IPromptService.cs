using sisyphish.Discord.Models;
using sisyphish.Sisyphish.Models;

namespace sisyphish.Sisyphish.Services;

public interface IPromptService
{
    Task<Prompt?> CreatePrompt(DiscordInteraction interaction, Expedition expedition);
    Task<InitPromptResult?> InitPrompt(DiscordInteraction interaction);
    Task<Prompt?> GetPrompt(DiscordInteraction interaction);
    Task LockPrompt(Prompt prompt);
    Task DeletePrompt(DiscordInteraction interaction);
}
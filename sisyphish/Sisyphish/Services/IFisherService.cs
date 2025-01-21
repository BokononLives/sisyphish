using sisyphish.Discord.Models;
using sisyphish.Sisyphish.Models;

namespace sisyphish.Sisyphish.Services;

public interface IFisherService
{
    Task DeleteFisher(DiscordInteraction interaction);
    Task<Fisher?> GetFisher(DiscordInteraction interaction);
    Task<Fisher?> CreateFisher(DiscordInteraction interaction);
    Task LockFisher(Fisher fisher);
    Task UnlockFisher(Fisher? fisher);
    Task AddFish(Fisher fisher, FishType fishType, long fishSize);
    Task AddItem(Fisher fisher, ItemType item);
    Task<InitFisherResult?> InitFisher(DiscordInteraction interaction);
    Task CreatePrompt(DiscordInteraction interaction, Expedition expedition);
    Task<InitPromptResult?> InitPrompt(DiscordInteraction interaction);
    Task<Prompt?> GetPrompt(DiscordInteraction interaction);
    Task LockPrompt(Prompt prompt);
    Task DeletePrompt(DiscordInteraction interaction);
}
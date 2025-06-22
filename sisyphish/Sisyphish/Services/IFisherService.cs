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
}
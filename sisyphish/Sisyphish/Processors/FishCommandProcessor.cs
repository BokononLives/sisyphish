using sisyphish.Discord;
using sisyphish.Discord.Models;
using sisyphish.GoogleCloud;
using sisyphish.Sisyphish.Models;
using sisyphish.Sisyphish.Services;
using sisyphish.Tools;

namespace sisyphish.Sisyphish.Processors;

public class FishCommandProcessor : ICommandProcessor
{
    private readonly ICloudTasksService _cloudTasks;
    private readonly IDiscordService _discord;
    private readonly IFisherService _fisherService;
    private readonly ILogger<FishCommandProcessor> _logger;

    public FishCommandProcessor(ICloudTasksService cloudTasks, IDiscordService discord, IFisherService fisherService, ILogger<FishCommandProcessor> logger)
    {
        _cloudTasks = cloudTasks;
        _discord = discord;
        _fisherService = fisherService;
        _logger = logger;
    }

    public DiscordCommandName? Command => DiscordCommandName.Fish;

    public async Task<IDiscordInteractionResponse> ProcessInitialCommand(DiscordInteraction interaction)
    {
        var eventRoll = Random.Shared.Next(1, 21);
        if (eventRoll == 20)
        {
            interaction.IsLucky = true;
        }
        
        await _discord.DeferResponse(interaction, isEphemeral: interaction.IsLucky == true);
        await _cloudTasks.CreateHttpPostTask($"{Config.PublicBaseUrl}/sisyphish/fish", interaction);
        
        var response = new DeferredDiscordInteractionResponse();
        return response;
    }

    public async Task ProcessFollowUpToCommand(DiscordInteraction interaction)
    {
        var initFisherResult = await _fisherService.InitFisher(interaction);
        var fisher = initFisherResult?.Fisher;

        try
        {
            if (fisher == null)
            {
                await ServeError(interaction, "An unexpected error occurred, please try again later!");
            }
            else if (initFisherResult?.InitSuccess != true)
            {
                await ServeError(interaction, $"<@{fisher.DiscordUserId}>, you are sending messages too quickly, please try again in a moment!");
            }
            else
            {
                var expedition =
                    interaction.IsLucky == true
                        ? GetLucky(fisher)
                        : GoFish(fisher);
                
                if (expedition == null)
                {
                    await ServeError(interaction, "An unexpected error occurred, please try again later!");
                }
                else
                {
                    switch (expedition.Event)
                    {
                        case Event.FoundTreasureChest:
                            await _fisherService.CreatePrompt(interaction, expedition);
                            break;
                        default:
                            break;
                    }
                    
                    if (expedition.CaughtFish == true)
                    {
                        await _fisherService.AddFish(fisher, expedition.FishType!.Value, (long)expedition.FishSize!);
                    }

                    var content = expedition.GetContent(fisher);
                    var components = expedition.GetComponents();

                    await _discord.EditResponse(interaction, content, components);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Fish command!");
        }
        
        await _fisherService.UnlockFisher(fisher);
    }

    private async Task ServeError(DiscordInteraction interaction, string errorMessage)
    {
        await _discord.EditResponse(interaction, "I sure do love fishin'!", []);
        await _discord.SendFollowupResponse(interaction, errorMessage, [], false);
        await _discord.DeleteResponse(interaction);
    }

    private static Expedition? GetLucky(Fisher? fisher)
    {
        var expedition = new Expedition(fisher?.DiscordUserId)
        {
            Event = Event.None,
            FishSize = null,
            CaughtFish = false
        };

        expedition.Event = Event.FoundTreasureChest;
        return expedition;
    }

    private static Expedition? GoFish(Fisher? fisher)
    {
        if (fisher == null)
        {
            return null;
        }

        var expedition = new Expedition(fisher?.DiscordUserId)
        {
            Event = Event.None,
            FishSize = null,
            CaughtFish = false
        };

        var biteRoll = Random.Shared.Next(1, 11);
        if (biteRoll <= 4)
        {
            return expedition;
        }

        var fishType = FishType.BettaTester;
        expedition.FishType = fishType;

        var fishSize = 0;
        int fishRoll;

        do
        {
            fishRoll = Random.Shared.Next(1, 11);
            fishSize += fishRoll;
        } while (fishRoll == 10);

        expedition.FishSize = fishSize;

        var reelStrength = 2;
        int reelRoll;

        do
        {
            reelRoll = Random.Shared.Next(1, 10);
            reelStrength += reelRoll;
        } while (reelRoll == 10);

        if (reelStrength >= fishSize)
        {
            expedition.CaughtFish = true;
        }

        return expedition;
    }
}
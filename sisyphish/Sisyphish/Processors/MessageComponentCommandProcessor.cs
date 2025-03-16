using sisyphish.Discord;
using sisyphish.Discord.Models;
using sisyphish.GoogleCloud;
using sisyphish.Sisyphish.Models;
using sisyphish.Sisyphish.Services;
using sisyphish.Tools;

namespace sisyphish.Sisyphish.Processors;

public class MessageComponentCommandProcessor : ICommandProcessor
{
    private readonly ICloudTasksService _cloudTasks;
    private readonly IDiscordService _discord;
    private readonly IFisherService _fisherService;
    private readonly ILogger<MessageComponentCommandProcessor> _logger;

    public MessageComponentCommandProcessor(ICloudTasksService cloudTasks, IDiscordService discord, IFisherService fisherService, ILogger<MessageComponentCommandProcessor> logger)
    {
        _cloudTasks = cloudTasks;
        _discord = discord;
        _fisherService = fisherService;
        _logger = logger;
    }

    public DiscordCommandName? Command => null;

    public async Task<IDiscordInteractionResponse> ProcessInitialCommand(DiscordInteraction interaction)
    {
        if (interaction.UserId != interaction.PromptUserId)
        {
            return new DiscordInteractionResponse
            {
                ContentType = DiscordInteractionResponseContentType.ChannelMessageWithSource,
                Data = new DiscordInteractionResponseData
                {
                    Flags = DiscordInteractionResponseFlags.Ephemeral,
                    Content = "An unexpected error occurred, please try again later!"
                }
            };
        }
        
        await _discord.DeferResponse(interaction, isEphemeral: false);
        await _discord.DeleteResponse(interaction, interaction.Message?.Id);
        await _cloudTasks.CreateHttpPostTask($"{Config.PublicBaseUrl}/sisyphish/event", interaction);

        var response = new DeferredDiscordInteractionResponse();
        return response;
    }

    public async Task ProcessFollowUpToCommand(DiscordInteraction interaction)
    {
        var initFisherResult = await _fisherService.InitFisher(interaction);
        var fisher = initFisherResult?.Fisher;
        
        var initPromptResult = await _fisherService.InitPrompt(interaction);
        var prompt = initPromptResult?.Prompt;

        try
        {
            if (fisher == null || prompt == null || initFisherResult?.InitSuccess != true || initPromptResult?.InitSuccess != true)
            {
                await _discord.DeleteResponse(interaction);
            }
            else
            {
                switch (prompt?.Event)
                {
                    case Event.ResetData:
                        if (interaction.PromptResponse == "confirm")
                        {
                            var content = $"Bye, <@{interaction.UserId}>!";

                            await _fisherService.DeleteFisher(interaction);
                            await _discord.EditResponse(interaction, content, []);

                            fisher = null;
                        }
                        else
                        {
                            var content = $"Fish on, <@{interaction.UserId}>.";

                            await _discord.EditResponse(interaction, content, []);
                        }

                        await _fisherService.DeletePrompt(interaction);

                        break;
                        
                    case Event.FoundTreasureChest:
                        var item = interaction.PromptResponse == "open"
                            ? (ItemType?)Random.Shared.GetItems(Enum.GetValues<ItemType>(), 1).Single()
                            : null;
                        
                        if (item == null)
                        {
                            var content = $"You get nothing!";

                            await _discord.EditResponse(interaction, content, []);
                        }
                        else
                        {
                            var content = $"Inside the chest was: 1 {item}!";

                            await _fisherService.AddItem(fisher, item.Value);
                            await _discord.EditResponse(interaction, content, []);
                        }
                        
                        await _fisherService.DeletePrompt(interaction);
                        
                        break;
                    default:
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Message Component!");
        }
        
        await _fisherService.UnlockFisher(fisher);
    }
}
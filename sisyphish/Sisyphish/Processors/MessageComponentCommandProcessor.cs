using sisyphish.Discord;
using sisyphish.Discord.Models;
using sisyphish.GoogleCloud;
using sisyphish.Sisyphish.Services;

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

        try
        {
            //TODO
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Message Component!");
        }
        
        await _fisherService.UnlockFisher(fisher);
    }
}
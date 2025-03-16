using sisyphish.Discord;
using sisyphish.Discord.Models;
using sisyphish.GoogleCloud;
using sisyphish.Sisyphish.Models;
using sisyphish.Sisyphish.Services;
using sisyphish.Tools;

namespace sisyphish.Sisyphish.Processors;

public class ResetCommandProcessor : ICommandProcessor
{
    private readonly ICloudTasksService _cloudTasks;
    private readonly IDiscordService _discord;
    private readonly IFisherService _fisherService;

    public ResetCommandProcessor(ICloudTasksService cloudTasks, IDiscordService discord, IFisherService fisherService)
    {
        _cloudTasks = cloudTasks;
        _discord = discord;
        _fisherService = fisherService;
    }

    public DiscordCommandName? Command => DiscordCommandName.Reset;

    public async Task<IDiscordInteractionResponse> ProcessInitialCommand(DiscordInteraction interaction)
    {   
        await _discord.DeferResponse(interaction, isEphemeral: true);
        await _cloudTasks.CreateHttpPostTask($"{Config.PublicBaseUrl}/sisyphish/reset", interaction);
        
        var response = new DeferredDiscordInteractionResponse();
        return response;
    }

    public async Task ProcessFollowUpToCommand(DiscordInteraction interaction)
    {
        var expedition = new Expedition(interaction.UserId)
        {
            Event = Event.ResetData
        };

        await _fisherService.CreatePrompt(interaction, expedition);

        var content = expedition.GetContent();
        var components = expedition.GetComponents();

        await _discord.EditResponse(interaction, content, components);
    }
}
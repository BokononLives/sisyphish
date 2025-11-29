using sisyphish.Discord;
using sisyphish.Discord.Models;
using sisyphish.GoogleCloud.CloudTasks;
using sisyphish.Sisyphish.Models;
using sisyphish.Sisyphish.Services;
using sisyphish.Tools;

namespace sisyphish.Sisyphish.Processors;

public class ResetCommandProcessor : IResetCommandProcessor
{
    private readonly ICloudTasksService _cloudTasks;
    private readonly IDiscordService _discord;
    private readonly IPromptService _promptService;

    public ResetCommandProcessor(ICloudTasksService cloudTasks, IDiscordService discord, IPromptService promptService)
    {
        _cloudTasks = cloudTasks;
        _discord = discord;
        _promptService = promptService;
    }

    public DiscordCommandName Command => DiscordCommandName.Reset;

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

        await _promptService.CreatePrompt(interaction, expedition);

        var content = expedition.GetContent();
        var components = expedition.GetComponents();

        await _discord.EditResponse(interaction, content, components);
    }
}

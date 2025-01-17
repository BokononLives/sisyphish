using sisyphish.Discord;
using sisyphish.Discord.Models;
using sisyphish.GoogleCloud;

namespace sisyphish.Sisyphish.Processors;

public class ResetCommandProcessor : ICommandProcessor
{
    private readonly ICloudTasksService _cloudTasks;
    private readonly IDiscordService _discord;

    public ResetCommandProcessor(ICloudTasksService cloudTasks, IDiscordService discord)
    {
        _cloudTasks = cloudTasks;
        _discord = discord;
    }

    public DiscordCommandName? Command => DiscordCommandName.Reset;

    public async Task<IDiscordInteractionResponse> ProcessInitialCommand(DiscordInteraction interaction)
    {   
        await _discord.DeferResponse(interaction, isEphemeral: false);
        await _cloudTasks.CreateHttpPostTask($"{Config.PublicBaseUrl}/sisyphish/reset", interaction);
        
        var response = new DeferredDiscordInteractionResponse();
        return response;
    }
}
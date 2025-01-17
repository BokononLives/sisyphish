using sisyphish.Discord;
using sisyphish.Discord.Models;
using sisyphish.GoogleCloud;

namespace sisyphish.Sisyphish.Processors;

public class FishCommandProcessor : ICommandProcessor
{
    private readonly ICloudTasksService _cloudTasks;
    private readonly IDiscordService _discord;

    public FishCommandProcessor(ICloudTasksService cloudTasks, IDiscordService discord)
    {
        _cloudTasks = cloudTasks;
        _discord = discord;
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
}
using sisyphish.Discord;
using sisyphish.Discord.Models;
using sisyphish.GoogleCloud;

namespace sisyphish.Sisyphish.Processors;

public class MessageComponentCommandProcessor : ICommandProcessor
{
    private readonly ICloudTasksService _cloudTasks;
    private readonly IDiscordService _discord;

    public MessageComponentCommandProcessor(ICloudTasksService cloudTasks, IDiscordService discord)
    {
        _cloudTasks = cloudTasks;
        _discord = discord;
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
}
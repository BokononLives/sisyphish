using sisyphish.Tools.Discord.Models;

namespace sisyphish.Tools.Discord;

public class DiscordInteractionProcessor : IDiscordInteractionProcessor
{
    public async Task<IDiscordInteractionResponse> ProcessDiscordInteraction(DiscordInteraction interaction)
    {
        return (interaction?.Type) switch
        {
            DiscordInteractionType.Ping => Pong(),
            DiscordInteractionType.ApplicationCommand => ProcessApplicationCommand(interaction),
            null => new DiscordInteractionErrorResponse { Error = "Interaction type is required" },
            _ => new DiscordInteractionErrorResponse { Error = "Invalid interaction type" },
        };
    }

    private static DiscordInteractionResponse Pong()
    {
        return new DiscordInteractionResponse { ContentType = DiscordInteractionResponseContentType.Pong };
    }

    private IDiscordInteractionResponse ProcessApplicationCommand(DiscordInteraction interaction)
    {
        return (interaction.Data?.Name) switch
        {
            DiscordCommandName.Fish => new DiscordInteractionResponse
            {
                ContentType = DiscordInteractionResponseContentType.ChannelMessageWithSource,
                Data = new DiscordInteractionResponseData
                {
                    Content = "Nothing's biting yet..."
                }
            },
            _ => new DiscordInteractionErrorResponse
            {
                Error = "Invalid command name"
            },
        };
    }
}
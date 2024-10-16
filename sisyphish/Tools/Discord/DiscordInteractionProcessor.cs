using sisyphish.Tools.Discord.Models;

namespace sisyphish.Tools.Discord;

public class DiscordInteractionProcessor : IDiscordInteractionProcessor
{
    public async Task<IDiscordInteractionResponse> ProcessDiscordInteraction(DiscordInteraction interaction)
    {
        switch (interaction?.Type)
        {
            case null:
                return new DiscordInteractionErrorResponse { Error = "Interaction type is required" };
            case DiscordInteractionType.Ping:
                return Pong();
            case DiscordInteractionType.ApplicationCommand:
                return ProcessApplicationCommand(interaction);
            default:
                return new DiscordInteractionErrorResponse { Error = "Invalid interaction type" };
        }
    }

    private DiscordInteractionResponse Pong()
    {
        return new DiscordInteractionResponse { ContentType = DiscordInteractionResponseContentType.Pong };
    }

    private IDiscordInteractionResponse ProcessApplicationCommand(DiscordInteraction interaction)
    {
        return (interaction.Data?.Name?.ToLower()) switch
        {
            "fish" => new DiscordInteractionResponse
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
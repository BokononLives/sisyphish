using sisyphish.Discord.Models;
using sisyphish.Sisyphish.Processors;

namespace sisyphish.Controllers;

public class HomeController
{
    private readonly IEnumerable<ICommandProcessor> _commandProcessors;

    public HomeController(IEnumerable<ICommandProcessor> commandProcessors)
    {
        _commandProcessors = commandProcessors;
    }
    public string Get()
    {
        return "Hello world!";
    }

    public async Task<IDiscordInteractionResponse> Post(DiscordInteraction interaction)
    {
        var response = (interaction?.Type) switch
        {
            DiscordInteractionType.Ping => Pong(),
            DiscordInteractionType.ApplicationCommand => await ProcessApplicationCommand(interaction),
            DiscordInteractionType.MessageComponent => await ProcessMessageComponent(interaction),
            null => new DiscordInteractionErrorResponse { Error = "Interaction type is required" },
            _ => new DiscordInteractionErrorResponse { Error = "Invalid interaction type" },
        };
        
        return response;
    }

    private static DiscordInteractionResponse Pong()
    {
        return new DiscordInteractionResponse { ContentType = DiscordInteractionResponseContentType.Pong };
    }

    private async Task<IDiscordInteractionResponse> ProcessApplicationCommand(DiscordInteraction interaction)
    {
        var commandProcessors = _commandProcessors
            .Where(p => p.Command == interaction.Data?.Name)
            .ToList();
        
        var result = await ProcessInitialCommand(interaction, commandProcessors);
        return result;
    }

    private async Task<IDiscordInteractionResponse> ProcessMessageComponent(DiscordInteraction interaction)
    {
        var commandProcessors = _commandProcessors
            .OfType<MessageComponentCommandProcessor>()
            .ToList<ICommandProcessor>();
        
        var result = await ProcessInitialCommand(interaction, commandProcessors);
        return result;
    }

    private async Task<IDiscordInteractionResponse> ProcessInitialCommand(DiscordInteraction interaction, List<ICommandProcessor> processors)
    {
        if (processors.Count != 1)
        {
            return new DiscordInteractionErrorResponse { Error = "An unexpected error occurred, please try again later!" };
        }

        var commandProcessor = processors.Single();

        var result = await commandProcessor.ProcessInitialCommand(interaction);
        return result;
    }
}
using sisyphish.Discord.Models;
using sisyphish.Extensions;
using sisyphish.Filters;
using sisyphish.Sisyphish.Processors;
using sisyphish.Tools;

namespace sisyphish.Controllers;

public class DiscordInteractionController(IEnumerable<ICommandProcessor> commandProcessors) : IController<DiscordInteraction, IDiscordInteractionResponse>
{
    public static string Path => "/";

    public static void MapRoute(WebApplication app)
        => app.MapPost("/", async (HttpContext context, DiscordInteractionController controller) =>
            {
                var interaction = await context.Request.ReadFromJsonAsync(SnakeCaseJsonContext.Default.DiscordInteraction);
                if (interaction == null)
                {
                    return Results.BadRequest("Invalid request");
                }

                var response = await controller.Execute(interaction);

                return response.ToResult();
            }).AddEndpointFilter<DiscordFilter>();

    public async Task<IDiscordInteractionResponse> Execute(DiscordInteraction interaction)
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
        var matchingCommandProcessors = commandProcessors
            .Where(p => p.Command == interaction.Data?.Name)
            .ToList();

        var result = await ProcessInitialCommand(interaction, matchingCommandProcessors);
        return result;
    }

    private async Task<IDiscordInteractionResponse> ProcessMessageComponent(DiscordInteraction interaction)
    {
        var matchingCommandProcessors = commandProcessors
            .OfType<MessageComponentCommandProcessor>()
            .ToList<ICommandProcessor>();

        var result = await ProcessInitialCommand(interaction, matchingCommandProcessors);
        return result;
    }

    private static async Task<IDiscordInteractionResponse> ProcessInitialCommand(DiscordInteraction interaction, List<ICommandProcessor> processors)
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

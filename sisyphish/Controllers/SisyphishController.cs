using sisyphish.Discord.Models;
using sisyphish.Filters;
using sisyphish.Sisyphish.Processors;
using sisyphish.Tools;

namespace sisyphish.Controllers;

public abstract class SisyphishController(ICommandProcessor commandProcessor) : IController<DiscordInteraction, string>
{
    public static string Path => throw new NotImplementedException();

    public static void MapRoute(WebApplication app)
        => app.MapPost(Path, async (HttpContext context, FishController controller) =>
            {
                var interaction = await context.Request.ReadFromJsonAsync(SnakeCaseJsonContext.Default.DiscordInteraction);
                if (interaction == null)
                {
                    return Results.BadRequest("Invalid request");
                }

                var result = await controller.Execute(interaction);

                return Results.Ok(result);
            }).AddEndpointFilter<GoogleCloudFilter>();

    public async Task<string> Execute(DiscordInteraction interaction)
    {
        await commandProcessor.ProcessFollowUpToCommand(interaction);

        return "OK";
    }
}

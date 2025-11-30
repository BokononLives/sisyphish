using sisyphish.Discord.Models;
using sisyphish.Filters;
using sisyphish.Sisyphish.Processors;
using sisyphish.Tools;

namespace sisyphish.Controllers;

public abstract class SisyphishController(ICommandProcessor commandProcessor)
{
    public static void MapRoute<TController>(WebApplication app) where TController : SisyphishController, IController<DiscordInteraction, string>
        => app.MapPost(TController.Path, async (HttpContext context) =>
        {
            var controller = context.RequestServices.GetRequiredService<TController>();
            
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

using sisyphish.Discord.Models;
using sisyphish.Sisyphish.Processors;

namespace sisyphish.Controllers;

public class ResetController(IResetCommandProcessor commandProcessor) : SisyphishController(commandProcessor), IController<DiscordInteraction, string>
{
    public static string Path => "sisyphish/reset";

    public static void MapRoute(WebApplication app) => MapRoute<ResetController>(app);
}

using sisyphish.Discord.Models;
using sisyphish.Sisyphish.Processors;

namespace sisyphish.Controllers;

public class FishController(IFishCommandProcessor commandProcessor) : SisyphishController(commandProcessor), IController<DiscordInteraction, string>
{
    public static string Path => "sisyphish/fish";

    public static void MapRoute(WebApplication app) => MapRoute<FishController>(app);
}

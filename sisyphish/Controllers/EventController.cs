using sisyphish.Discord.Models;
using sisyphish.Sisyphish.Processors;

namespace sisyphish.Controllers;

public class EventController(IMessageComponentCommandProcessor commandProcessor) : SisyphishController(commandProcessor), IController<DiscordInteraction, string>
{
    public static string Path => "sisyphish/event";

    public static void MapRoute(WebApplication app) => MapRoute<EventController>(app);
}

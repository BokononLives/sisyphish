using sisyphish.Sisyphish.Processors;

namespace sisyphish.Controllers;

public class EventController(IMessageComponentCommandProcessor commandProcessor) : SisyphishController(commandProcessor)
{
    public static new string Path => "sisyphish/event";
}

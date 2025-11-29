using sisyphish.Sisyphish.Processors;

namespace sisyphish.Controllers;

public class FishController(IFishCommandProcessor commandProcessor) : SisyphishController(commandProcessor)
{
    public static new string Path => "sisyphish/fish";
}

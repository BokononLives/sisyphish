using sisyphish.Sisyphish.Processors;

namespace sisyphish.Controllers;

public class ResetController(IResetCommandProcessor commandProcessor) : SisyphishController(commandProcessor)
{
    public static new string Path => "sisyphish/reset";
}

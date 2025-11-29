namespace sisyphish.Controllers;

public interface IBaseController
{
    abstract static string Path { get; }
    abstract static void MapRoute(WebApplication app);
}

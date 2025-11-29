namespace sisyphish.Controllers;

public class HelloWorldController : IController<string>
{
    public static string Path => "/";

    public static void MapRoute(WebApplication app)
        => app.MapGet(Path, async (HelloWorldController controller) =>
            {
                var result = await controller.Execute();

                return Results.Ok(result);
            });

    public async Task<string> Execute() => await Task.FromResult("Hello world!");
}

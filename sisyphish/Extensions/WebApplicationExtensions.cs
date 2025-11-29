using sisyphish.Controllers;
using sisyphish.Tools;

namespace sisyphish.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication MapRoute<TController>(this WebApplication app) where TController : IBaseController
    {
        TController.MapRoute(app);

        return app;
    }

    public static WebApplication UseRequestTracking(this WebApplication app, out RequestTracker requestTracker)
    {
        requestTracker = app.Services.GetRequiredService<RequestTracker>();

        app.UseRequestTracking(requestTracker);

        return app;
    }

    private static WebApplication UseRequestTracking(this WebApplication app, RequestTracker requestTracker)
    {
        app.Use(async (context, next) =>
        {
            requestTracker.BeginRequest();

            try
            {
                await next();
            }
            finally
            {
                requestTracker.EndRequest();
            }
        });

        return app;
    }
}

using sisyphish.Controllers;
using sisyphish.Extensions;
using sisyphish.GoogleCloud.Logging;
using sisyphish.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Host
    .ConfigureHostOptions(options => options.ShutdownTimeout = TimeSpan.FromSeconds(Config.ShutdownTimeoutInSeconds));

builder.Services
    .AddAuthorization()
    .ConfigureHttpJsonOptions(options => JsonHelpers.SetupDefaultJsonSerializerOptions(options.SerializerOptions))
    .AddRequiredServices();

builder
    .AddLogging(out var logChannel);

var app = builder.Build();

var logService = app.Services.GetRequiredService<GoogleCloudLoggingBackgroundService>();
var processLogs = Task.Run(() => logService.ProcessLogs());

app
    .UseRequestTracking(out var requestTracker)
    .MapRoute<HelloWorldController>()
    .MapRoute<DiscordInteractionController>()
    .MapRoute<FishController>()
    .MapRoute<EventController>()
    .MapRoute<ResetController>()
    .UseAuthorization()
    .Use(async (context, next) =>
        {
            context.Request.EnableBuffering();
            await next();
        });

app.Lifetime.ApplicationStopping.Register(() =>
{
    Task.Run(async () =>
    {
        await requestTracker.WaitForAllRequestsToProcess();
        
        await Task.Delay(200);

        logChannel.Writer.Complete();
        await processLogs;
    });
});

app.Run(Config.Url);

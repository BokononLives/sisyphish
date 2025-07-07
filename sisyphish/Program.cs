using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.Extensions.Logging.Console;
using sisyphish.Controllers;
using sisyphish.Discord;
using sisyphish.Extensions;
using sisyphish.Filters;
using sisyphish.GoogleCloud.Authentication;
using sisyphish.GoogleCloud.CloudTasks;
using sisyphish.GoogleCloud.Firestore;
using sisyphish.GoogleCloud.Logging;
using sisyphish.Sisyphish.Processors;
using sisyphish.Sisyphish.Services;
using sisyphish.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();

builder.Host.ConfigureHostOptions(options => options.ShutdownTimeout = TimeSpan.FromSeconds(30));

var setUpJsonSerializerOptions = (JsonSerializerOptions options) =>
{
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
};

builder.Services.ConfigureHttpJsonOptions(options =>
{
    setUpJsonSerializerOptions(options.SerializerOptions);
});

builder.Services.AddHttpClient<IGoogleCloudAuthenticationService, GoogleCloudAuthenticationService>(client =>
{
    client.BaseAddress = new Uri(Config.GoogleMetadataBaseUrl);
    client.DefaultRequestHeaders.Add("Metadata-Flavor", "Google");
});

builder.Services.AddHttpClient<ICloudTasksService, CloudTasksService>(client =>
{
    client.BaseAddress = new Uri(Config.GoogleTasksBaseUrl);
});

builder.Services.AddHttpClient<IFirestoreService, FirestoreService>(client =>
{
    client.BaseAddress = new Uri(Config.GoogleFirestoreBaseUrl);
});

builder.Services.AddHttpClient(nameof(GoogleCloudFilter), client =>
{
    client.BaseAddress = new Uri(Config.GoogleCertsBaseUrl);
});

builder.Services.AddHttpClient<IDiscordService, DiscordService>(client =>
{
    client.BaseAddress = new Uri(Config.DiscordBaseUrl);
});

builder.Services.AddScoped<DiscordFilter>();
builder.Services.AddScoped<IFisherService, FirestoreDbFisherService>();
builder.Services.AddScoped<IPromptService, FirestoreDbPromptService>();
builder.Services.AddScoped<IEnumerable<ICommandProcessor>>(x =>
[
    new FishCommandProcessor(x.GetRequiredService<ICloudTasksService>(), x.GetRequiredService<IDiscordService>(), x.GetRequiredService<IFisherService>(), x.GetRequiredService<IPromptService>(), x.GetRequiredService<ILogger<FishCommandProcessor>>()),
    new MessageComponentCommandProcessor(x.GetRequiredService<ICloudTasksService>(), x.GetRequiredService<IDiscordService>(), x.GetRequiredService<IFisherService>(), x.GetRequiredService<IPromptService>(), x.GetRequiredService<ILogger<MessageComponentCommandProcessor>>()),
    new ResetCommandProcessor(x.GetRequiredService<ICloudTasksService>(), x.GetRequiredService<IDiscordService>(), x.GetRequiredService<IPromptService>())
]);
builder.Services.AddScoped<HomeController>();
builder.Services.AddScoped<SisyphishController>();

var logChannel = Channel.CreateUnbounded<Log>();
var logReader = logChannel.Reader;
var logWriter = logChannel.Writer;

var requestTracker = new RequestTracker();

builder.Services.AddSingleton(logReader);
builder.Services.AddHttpClient<IGoogleCloudLoggingService, GoogleCloudLoggingService>(client =>
{
    client.BaseAddress = new Uri(Config.GoogleLoggingBaseUrl);
}).RemoveAllLoggers();

var logProvider = new GoogleCloudLoggerProvider(logWriter);
builder.Logging
    .ClearProviders()
    .AddProvider(logProvider)
    .AddJsonConsole()
    .AddFilter<ConsoleLoggerProvider>("Microsoft.Hosting.Lifetime", LogLevel.Information)
    .AddFilter<ConsoleLoggerProvider>("Microsoft.AspNetCore.Diagnostics", LogLevel.Warning);

builder.Services.AddSingleton<GoogleCloudLoggingBackgroundService>();

var app = builder.Build();

var logService = app.Services.GetRequiredService<GoogleCloudLoggingBackgroundService>();

var processLogs = Task.Run(() => logService.ProcessLogs());

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

app.MapGet("/", (HomeController controller) =>
{
    return controller.Get();
});

app.MapPost("/", async (HttpContext context, HomeController controller) =>
{
    var interaction = await context.Request.ReadFromJsonAsync(SnakeCaseJsonContext.Default.DiscordInteraction);
    if (interaction == null)
    {
        return Results.BadRequest("Invalid request");
    }

    var response = await controller.Post(interaction);

    return response.ToResult();
}).AddEndpointFilter<DiscordFilter>();

app.MapPost("sisyphish/fish", async (HttpContext context, SisyphishController controller) =>
{
    var interaction = await context.Request.ReadFromJsonAsync(SnakeCaseJsonContext.Default.DiscordInteraction);
    if (interaction == null)
    {
        return Results.BadRequest("Invalid request");
    }

    await controller.ProcessFishCommand(interaction);

    return Results.Ok();
}).AddEndpointFilter<GoogleCloudFilter>();

app.MapPost("sisyphish/event", async (HttpContext context, SisyphishController controller) =>
{
    var interaction = await context.Request.ReadFromJsonAsync(SnakeCaseJsonContext.Default.DiscordInteraction);
    if (interaction == null)
    {
        return Results.BadRequest("Invalid request");
    }

    await controller.ProcessEvent(interaction);

    return Results.Ok();
}).AddEndpointFilter<GoogleCloudFilter>();

app.MapPost("sisyphish/reset", async (HttpContext context, SisyphishController controller) =>
{
    var interaction = await context.Request.ReadFromJsonAsync(SnakeCaseJsonContext.Default.DiscordInteraction);
    if (interaction == null)
    {
        return Results.BadRequest("Invalid request");
    }

    await controller.ProcessResetCommand(interaction);

    return Results.Ok();
}).AddEndpointFilter<GoogleCloudFilter>();

app.UseAuthorization();

app.Use(async (context, next) =>
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

        logWriter.Complete();
        await processLogs;
    });
});

app.Run(Config.Url);
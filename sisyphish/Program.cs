using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Cloud.Diagnostics.Common;
using Google.Cloud.Firestore;
using sisyphish;
using sisyphish.Controllers;
using sisyphish.Discord;
using sisyphish.Extensions;
using sisyphish.Filters;
using sisyphish.GoogleCloud;
using sisyphish.Sisyphish.Processors;
using sisyphish.Sisyphish.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();

builder.Host.ConfigureHostOptions(options => options.ShutdownTimeout = TimeSpan.FromSeconds(30));

builder.Logging.AddGoogle(new LoggingServiceOptions
{
    ProjectId = Config.GoogleProjectId,
    Options = LoggingOptions.Create(logLevel: LogLevel.Debug)
});

var setUpJsonSerializerOptions = (JsonSerializerOptions options) =>
{
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
};

builder.Services.ConfigureHttpJsonOptions(options =>
{
    setUpJsonSerializerOptions(options.SerializerOptions);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped(serviceProvider => FirestoreDb.Create(Config.GoogleProjectId));

builder.Services.AddScoped<ICloudTasksService, CloudTasksService>();
builder.Services.AddScoped<DiscordFilter>();
builder.Services.AddScoped<IDiscordService, DiscordService>();
builder.Services.AddScoped<IFisherService, FirestoreDbFisherService>();
builder.Services.AddScoped<IEnumerable<ICommandProcessor>>(x =>
[
    new FishCommandProcessor(x.GetRequiredService<ICloudTasksService>(), x.GetRequiredService<IDiscordService>(), x.GetRequiredService<IFisherService>(), x.GetRequiredService<ILogger<FishCommandProcessor>>()),
    new MessageComponentCommandProcessor(x.GetRequiredService<ICloudTasksService>(), x.GetRequiredService<IDiscordService>(), x.GetRequiredService<IFisherService>(), x.GetRequiredService<ILogger<MessageComponentCommandProcessor>>()),
    new ResetCommandProcessor(x.GetRequiredService<ICloudTasksService>(), x.GetRequiredService<IDiscordService>(), x.GetRequiredService<IFisherService>())
]);
builder.Services.AddScoped<HomeController>();
builder.Services.AddScoped<SisyphishController>();

var app = builder.Build();

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

app.UseHttpsRedirection();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();

    await next();
});

app.Run(Config.Url);
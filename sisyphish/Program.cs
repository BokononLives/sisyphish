using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
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

builder.Services.AddHttpClient<GoogleCloudFilter>(client =>
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

builder.Logging.ClearProviders();
builder.Services.AddHttpClient(nameof(GoogleCloudLoggerProvider), client =>
{
    client.BaseAddress = new Uri(Config.GoogleLoggingBaseUrl);
});
builder.Services.AddSingleton<ILoggerProvider>(x =>
{
    return new GoogleCloudLoggerProvider(
        x.GetRequiredService<IGoogleCloudAuthenticationService>(),
        x.GetRequiredService<IHttpClientFactory>());
});

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
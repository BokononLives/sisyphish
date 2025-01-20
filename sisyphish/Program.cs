using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Cloud.Diagnostics.Common;
using Google.Cloud.Firestore;
using Google.Cloud.Tasks.V2;
using sisyphish.Discord;
using sisyphish.Filters;
using sisyphish.GoogleCloud;
using sisyphish.Sisyphish.Processors;
using sisyphish.Sisyphish.Services;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        setUpJsonSerializerOptions(options.JsonSerializerOptions);
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped(serviceProvider => FirestoreDb.Create(Config.GoogleProjectId));
builder.Services.AddScoped(serviceProvider => new CloudTasksClientBuilder().Build());
builder.Services.AddScoped<ICloudTasksService, CloudTasksService>();
builder.Services.AddScoped<DiscordAttribute>();
builder.Services.AddScoped<IDiscordService, DiscordService>();
builder.Services.AddScoped<IFisherService, FirestoreDbFisherService>();

var commandProcessors = Assembly.GetAssembly(typeof(ICommandProcessor))?
    .GetTypes()
    .Where(type => typeof(ICommandProcessor).IsAssignableFrom(type) && !type.IsInterface)
    .ToList() ?? [];

foreach (var commandProcessorType in commandProcessors)
{
    builder.Services.AddScoped(typeof(ICommandProcessor), commandProcessorType);
}

var app = builder.Build();

if (Config.IsDevelopment)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();

    await next();
});

app.Run(Config.Url);
using Google.Cloud.BigQuery.V2;
using Google.Cloud.Firestore;
using Google.Cloud.Tasks.V2;
using sisyphish.Filters;
using sisyphish.GoogleCloud;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureHostOptions(options => options.ShutdownTimeout = TimeSpan.FromSeconds(30));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped(serviceProvider => BigQueryClient.Create(Config.GoogleProjectId));
builder.Services.AddScoped(serviceProvider => FirestoreDb.Create(Config.GoogleProjectId));
builder.Services.AddScoped(serviceProvider => new CloudTasksClientBuilder().Build());
builder.Services.AddScoped<ICloudTasksService, CloudTasksService>();
builder.Services.AddScoped<DiscordAttribute>();

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
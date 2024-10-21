using Google.Cloud.BigQuery.V2;
using Google.Cloud.Tasks.V2;
using sisyphish.GoogleCloud;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped(serviceProvider => BigQueryClient.Create(Config.GoogleProjectId));
builder.Services.AddScoped(serviceProvider => new CloudTasksClientBuilder().Build());
builder.Services.AddScoped<ICloudTasksService, CloudTasksService>();

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
using System.Text.Json;
using sisyphish.GoogleCloud.Authentication;
using sisyphish.Tools;

namespace sisyphish.GoogleCloud.Logging;

public class GoogleCloudLoggingService : GoogleCloudService, IGoogleCloudLoggingService
{
    public GoogleCloudLoggingService(IGoogleCloudAuthenticationService? authenticationService, HttpClient httpClient) : base(null, authenticationService, httpClient)
    {
    }

    public async Task Log(Log log) => await LogBatch([log]);

    public async Task LogBatch(List<Log> batch)
    {
        var logEntries = batch
            .Where(log => (log.Text ?? log.Exception?.Message) != null)
            .Select(log => new GoogleCloudLoggingLogEntry
            {
                Severity = MapSeverity(log.Level),
                Timestamp = log.Timestamp.ToString("o"),
                JsonPayload = new GoogleCloudLoggingJsonPayload
                {
                    Category = log.CategoryName,
                    Message = log.Text ?? log.Exception?.Message,
                    EventId = log.EventId.Id,
                    Exception = log.Exception?.ToString()
                }
            })
            .ToList();

        if (logEntries.Count == 0)
        {
            return;
        }

        try
        {
            await Authenticate();

            var logRequest = new GoogleCloudLoggingLogRequest
            {
                LogName = $"projects/{Config.GoogleProjectId}/logs/{Config.GoogleLoggingLogName}",
                Resource = new GoogleCloudLoggingLogResource
                {
                    Type = "cloud_run_revision",
                    Labels = new Dictionary<string, string>
                    {
                        { "project_id", Config.GoogleProjectId },
                        { "service_name", Config.GoogleCloudRunServiceName },
                        { "revision_name", Config.GoogleCloudRunRevisionName },
                        { "location", Config.GoogleLocation }
                    }
                },
                Entries = logEntries
            };

            var httpResponse = await _httpClient.PostAsJsonAsync(
                requestUri: "v2/entries:write",
                value: logRequest,
                jsonTypeInfo: CamelCaseJsonContext.Default.GoogleCloudLoggingLogRequest
            );

            try
            {
                httpResponse.EnsureSuccessStatusCode();
            }
            catch
            {
                Console.Error.WriteLine(await httpResponse.Content.ReadAsStringAsync());
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to write logs to Google Cloud Logging Api - {ex.Message}");

            var errorJson = JsonSerializer.Serialize(new FallbackErrorLog { Error = ex.ToString().Replace("\r", "").Replace("\n", "\\n") }, CamelCaseJsonContext.Default.FallbackErrorLog);
            Console.Error.WriteLine(errorJson);

            foreach (var log in batch)
            {
                Console.WriteLine(log.Text);
            }
        }
    }

    private static string MapSeverity(LogLevel? level)
    {
        return level switch
        {
            LogLevel.Trace or LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARNING",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRITICAL",
            _ => "DEFAULT"
        };
    }
}
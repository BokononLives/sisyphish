using System.Text.Json;
using System.Threading.Channels;
using sisyphish.GoogleCloud.Authentication;
using sisyphish.Tools;

namespace sisyphish.GoogleCloud.Logging;

public class GoogleCloudLoggingBackgroundService
{
    private readonly ChannelReader<Log> _logReader;
    private readonly IGoogleCloudLoggingService _logService;

    public GoogleCloudLoggingBackgroundService(ChannelReader<Log> logReader, IGoogleCloudLoggingService logService)
    {
        _logReader = logReader;
        _logService = logService;
    }

    public async Task ProcessLogs()
    {
        await foreach (var log in _logReader.ReadAllAsync())
        {
            try
            {
                await _logService.Log(log);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unable to write log to Google Cloud Logging Api - {ex.Message}");

                var errorJson = JsonSerializer.Serialize(new FallbackErrorLog { Error = ex.ToString().Replace("\r", "").Replace("\n", "\\n") }, CamelCaseJsonContext.Default.FallbackErrorLog);
                Console.Error.WriteLine(errorJson);

                Console.WriteLine(log.Text);
            }
        }
    }
}

public class FallbackErrorLog
{
    public string? Error { get; set; }
}

public interface IGoogleCloudLoggingService
{
    Task Log(Log log);
}

public class GoogleCloudLoggingService : GoogleCloudService, IGoogleCloudLoggingService
{
    public GoogleCloudLoggingService(IGoogleCloudAuthenticationService? authenticationService, HttpClient httpClient) : base(null, authenticationService, httpClient)
    {
    }

    public async Task Log(Log log)
    {
        log.Text ??= log.Exception?.Message;
        if (log.Text == null)
        {
            return;
        }

        try
        {
            await Authenticate();

            var logSeverity = MapSeverity(log.Level);
            var timestamp = log.Timestamp.ToString("o");

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
                Entries =
                [
                    new GoogleCloudLoggingLogEntry
                    {
                        Severity = logSeverity,
                        Timestamp = timestamp,
                        JsonPayload = new GoogleCloudLoggingJsonPayload
                        {
                            Category = log.CategoryName,
                            Message = log.Text,
                            EventId = log.EventId.Id,
                            Exception = log.Exception?.ToString()
                        }
                    }
                ]
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
            Console.Error.WriteLine($"Unable to write log to Google Cloud Logging Api - {ex.Message}");

            var errorJson = JsonSerializer.Serialize(new FallbackErrorLog { Error = ex.ToString().Replace("\r", "").Replace("\n", "\\n") }, CamelCaseJsonContext.Default.FallbackErrorLog);
            Console.Error.WriteLine(errorJson);

            Console.WriteLine(log.Text);
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

public class GoogleCloudLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ChannelWriter<Log> _logWriter;

    public GoogleCloudLogger(string categoryName, ChannelWriter<Log> logWriter)
    {
        _categoryName = categoryName;
        _logWriter = logWriter;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var logText = GetLogText(state, exception, formatter);
        if (logText == null)
        {
            return;
        }

        var log = new Log
        {
            CategoryName = _categoryName,
            EventId = eventId,
            Exception = exception,
            Level = logLevel,
            Text = logText,
            Timestamp = DateTime.UtcNow
        };

        _logWriter.TryWrite(log);
    }

    private static string? GetLogText<TState>(TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            var logText = formatter(state, exception);
            return logText;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to format log text - {ex.Message}");

            var errorJson = JsonSerializer.Serialize(new FallbackErrorLog { Error = ex.ToString().Replace("\r", "").Replace("\n", "\\n") }, CamelCaseJsonContext.Default.FallbackErrorLog);
            Console.Error.WriteLine(errorJson);

            return null;
        }
    }
}
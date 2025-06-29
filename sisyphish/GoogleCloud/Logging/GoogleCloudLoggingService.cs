using sisyphish.GoogleCloud.Authentication;
using sisyphish.Tools;

namespace sisyphish.GoogleCloud.Logging;

public partial class GoogleCloudLoggingService : GoogleCloudService, ILogger
{
    private readonly string _categoryName;

    public GoogleCloudLoggingService(string categoryName, IGoogleCloudAuthenticationService authenticationService, HttpClient httpClient) : base(null, authenticationService, httpClient)
    {
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= LogLevel.Information;

    public async void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var logText = GetLogText(state, exception, formatter);
        if (logText == null)
        {
            return;
        }

        try
        {
            await Authenticate();

            var logSeverity = logLevel.ToString().ToUpperInvariant();
            var timestamp = DateTime.UtcNow.ToString("o");

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
                            Category = _categoryName,
                            Message = logText,
                            EventId = eventId.Id,
                            Exception = exception?.ToString()
                        }
                    }
                ]
            };

            var httpResponse = await _httpClient.PostAsJsonAsync(
                requestUri: "entries:write",
                value: logRequest,
                jsonTypeInfo: CamelCaseJsonContext.Default.GoogleCloudLoggingLogRequest
            );

            httpResponse.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to write log to Google Cloud Logging Api - {ex.Message}");
            Console.WriteLine(logText);
        }
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
            Console.WriteLine($"Unable to format log text - {ex.Message}");
            return null;
        }
    }
}
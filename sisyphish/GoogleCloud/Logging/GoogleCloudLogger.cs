using System.Text.Json;
using System.Threading.Channels;
using sisyphish.Tools;

namespace sisyphish.GoogleCloud.Logging;

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
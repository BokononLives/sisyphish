namespace sisyphish.Tools;

public class Log
{
    public LogLevel Level { get; set; }
    public string? Text { get; set; }
    public string? CategoryName { get; set; }
    public DateTime Timestamp { get; set; }
    public EventId EventId { get; set; }
    public Exception? Exception { get; set; }
}
namespace sisyphish.GoogleCloud.Logging;

public class GoogleCloudLoggingLogEntry
{
    public string? Severity { get; set; }
    public string? Timestamp { get; set; }
    public GoogleCloudLoggingJsonPayload? JsonPayload { get; set; }
}

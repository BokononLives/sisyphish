namespace sisyphish.GoogleCloud.Logging;

public class GoogleCloudLoggingLogRequest
{
    public string? LogName { get; set; }
    public GoogleCloudLoggingLogResource? Resource { get; set; }
    public List<GoogleCloudLoggingLogEntry> Entries { get; set; } = [];
}
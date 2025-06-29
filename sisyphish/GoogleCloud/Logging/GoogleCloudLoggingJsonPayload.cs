namespace sisyphish.GoogleCloud.Logging;

public class GoogleCloudLoggingJsonPayload
{
    public string? Category { get; set; }
    public string? Message { get; set; }
    public int? EventId { get; set; }
    public string? Exception { get; set; }
}
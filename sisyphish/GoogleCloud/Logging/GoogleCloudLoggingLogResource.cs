namespace sisyphish.GoogleCloud.Logging;

public class GoogleCloudLoggingLogResource
{
    public string? Type { get; set; }
    public Dictionary<string, string> Labels { get; set; } = [];
}

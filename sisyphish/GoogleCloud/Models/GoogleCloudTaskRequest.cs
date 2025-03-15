namespace sisyphish.GoogleCloud.Models;

public class GoogleCloudTaskRequest
{
    public GoogleCloudTask? Task { get; set; }
}

public class GoogleCloudTask
{
    public GoogleCloudHttpRequest? HttpRequest { get; set; }
}

public class GoogleCloudHttpRequest
{
    public string? HttpMethod { get; set; }
    public string? Url { get; set; }
    public string? Body { get; set; }
}
namespace sisyphish.GoogleCloud.CloudTasks;

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
    public Dictionary<string, string>? Headers { get; set; }
    public GoogleCloudOidcToken? OidcToken { get; set; }
}

// public class GoogleCloudAuthorizationHeader
// {
//     public GoogleCloudOidcToken? OidcToken { get; set; }
// }

public class GoogleCloudOidcToken
{
    public string? ServiceAccountEmail { get; set; }
    public string? Audience { get; set; }
}

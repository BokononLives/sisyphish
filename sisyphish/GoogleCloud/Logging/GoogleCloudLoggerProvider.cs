using sisyphish.GoogleCloud.Authentication;

namespace sisyphish.GoogleCloud.Logging;

public class GoogleCloudLoggerProvider : ILoggerProvider
{
    private readonly IGoogleCloudAuthenticationService _authenticationService;
    private readonly HttpClient _httpClient;

    public GoogleCloudLoggerProvider(IGoogleCloudAuthenticationService authenticationService, HttpClient httpClient)
    {
        _authenticationService = authenticationService;
        _httpClient = httpClient;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new GoogleCloudLoggingService(categoryName, _authenticationService, _httpClient);
    }

    public void Dispose()
    {
    }
}

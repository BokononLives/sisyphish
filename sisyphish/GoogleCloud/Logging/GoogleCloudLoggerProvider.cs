using System.Net.Http;
using sisyphish.GoogleCloud.Authentication;

namespace sisyphish.GoogleCloud.Logging;

public class GoogleCloudLoggerProvider : ILoggerProvider
{
    private readonly IGoogleCloudAuthenticationService _authenticationService;
    private readonly IHttpClientFactory _httpClientFactory;

    public GoogleCloudLoggerProvider(IGoogleCloudAuthenticationService authenticationService, IHttpClientFactory httpClientFactory)
    {
        _authenticationService = authenticationService;
        _httpClientFactory = httpClientFactory;
    }

    public ILogger CreateLogger(string categoryName)
    {
        var httpClient = _httpClientFactory.CreateClient(nameof(GoogleCloudLoggerProvider));
        return new GoogleCloudLoggingService(categoryName, _authenticationService, httpClient);
    }

    public void Dispose()
    {
    }
}

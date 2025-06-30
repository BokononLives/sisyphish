using sisyphish.GoogleCloud.Authentication;

namespace sisyphish.GoogleCloud.Logging;

public class GoogleCloudLoggerProvider : ILoggerProvider
{
    private readonly IGoogleCloudAuthenticationService _authenticationService;
    private readonly IServiceProvider _serviceProvider;

    public GoogleCloudLoggerProvider(IGoogleCloudAuthenticationService authenticationService, IServiceProvider serviceProvider)
    {
        _authenticationService = authenticationService;
        _serviceProvider = serviceProvider;
    }

    public ILogger CreateLogger(string categoryName)
    {
        var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(nameof(GoogleCloudLoggerProvider));
        
        return new GoogleCloudLoggingService(categoryName, _authenticationService, httpClient);
    }

    public void Dispose()
    {
    }
}

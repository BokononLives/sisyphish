using sisyphish.GoogleCloud.Authentication;

namespace sisyphish.GoogleCloud;

public abstract class GoogleCloudService
{
    protected readonly ILogger<GoogleCloudService>? _logger;
    protected readonly IGoogleCloudAuthenticationService? _authenticationService;
    protected readonly HttpClient _httpClient;

    public GoogleCloudService(ILogger<GoogleCloudService>? logger, IGoogleCloudAuthenticationService? authenticationService, HttpClient httpClient)
    {
        _logger = logger;
        _authenticationService = authenticationService;
        _httpClient = httpClient;
    }
}
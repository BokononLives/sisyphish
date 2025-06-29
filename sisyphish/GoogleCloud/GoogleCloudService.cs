using System.Net.Http.Headers;
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

    protected async Task Authenticate()
    {
        if (_authenticationService == null)
        {
            _logger?.LogError(@$"Authentication service was unexpectedly null");

            throw new Exception("Unable to acquire Google Access token");
        }

        var accessToken = await _authenticationService.GetAccessToken();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
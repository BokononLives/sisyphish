using System.Net.Http.Headers;

namespace sisyphish.GoogleCloud.Authentication;

public class GoogleCloudAuthenticationHandler : DelegatingHandler
{
    private readonly IGoogleCloudAuthenticationService _authService;
    private readonly ILogger<GoogleCloudAuthenticationHandler> _logger;

    public GoogleCloudAuthenticationHandler(IGoogleCloudAuthenticationService authService, ILogger<GoogleCloudAuthenticationHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _authService.GetAccessToken();

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogError("Failed to retrieve Google Cloud access token.");
        }
        else
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

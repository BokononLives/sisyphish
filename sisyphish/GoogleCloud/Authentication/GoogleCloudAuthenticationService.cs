using sisyphish.Tools;

namespace sisyphish.GoogleCloud.Authentication;

public class GoogleCloudAuthenticationService : GoogleCloudService, IGoogleCloudAuthenticationService
{
    private string? _accessToken;
    private DateTime? _accessTokenExpirationDate;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public GoogleCloudAuthenticationService(ILogger<GoogleCloudAuthenticationService> logger, HttpClient httpClient) : base(logger, null, httpClient)
    {
    }

    public async Task<string> GetAccessToken()
    {
        var cachedAccessToken = GetCachedAccessToken();
        if (!string.IsNullOrWhiteSpace(cachedAccessToken))
        {
            return cachedAccessToken;
        }

        await _lock.WaitAsync();

        try
        {
            cachedAccessToken = GetCachedAccessToken();
            if (!string.IsNullOrWhiteSpace(cachedAccessToken))
            {
                return cachedAccessToken;
            }

            var httpResponse = await _httpClient.GetFromJsonAsync(
                requestUri: "computeMetadata/v1/instance/service-accounts/default/token",
                jsonTypeInfo: SnakeCaseJsonContext.Default.GoogleCloudAccessToken
            );

            if (string.IsNullOrWhiteSpace(httpResponse?.AccessToken))
            {
                _logger?.LogError(@$"Google Access Token was unexpectedly null:
                - response: {httpResponse}");

                throw new Exception("Unable to acquire Google Access token");
            }

            _accessToken = httpResponse.AccessToken;
            _accessTokenExpirationDate = DateTime.UtcNow.AddSeconds((httpResponse.ExpiresIn ?? 0) - 60);

            return _accessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private string? GetCachedAccessToken()
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && (_accessTokenExpirationDate == null || _accessTokenExpirationDate > DateTime.UtcNow))
        {
            return _accessToken;
        }

        return null;
    }
}

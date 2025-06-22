using System.Text;
using System.Text.Json;
using sisyphish.Discord.Models;
using sisyphish.Tools;

namespace sisyphish.GoogleCloud.CloudTasks;

public class CloudTasksService : ICloudTasksService
{
    private readonly ILogger<CloudTasksService> _logger;

    private string? _accessToken;
    private DateTime? _accessTokenExpirationDate;

    public CloudTasksService(ILogger<CloudTasksService> logger)
    {
        _logger = logger;
    }

    public async Task CreateHttpPostTask(string url, DiscordInteraction body)
    {
        var accessToken = await GetAccessToken();

        var serializedBody = JsonSerializer.Serialize(body, SnakeCaseJsonContext.Default.DiscordInteraction);
        var encodedBody = Convert.ToBase64String(Encoding.UTF8.GetBytes(serializedBody));

        var taskRequest = new GoogleCloudTaskRequest
        {
            Task = new GoogleCloudTask
            {
                HttpRequest = new GoogleCloudHttpRequest
                {
                    HttpMethod = "POST",
                    Url = url,
                    Body = encodedBody,
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" }
                    },
                    OidcToken = new GoogleCloudOidcToken
                    {
                        ServiceAccountEmail = Config.GoogleServiceAccount,
                        Audience = url
                    }
                }
            }
        };

        using var httpClient = new HttpClient { DefaultRequestHeaders = { { "Authorization", $"Bearer {accessToken}" } } };

        var taskResponse = await httpClient.PostAsJsonAsync(
            requestUri: $"{Config.GoogleTasksBaseUrl}/tasks",
            value: taskRequest,
            jsonTypeInfo: CamelCaseJsonContext.Default.GoogleCloudTaskRequest
        );

        var taskResponseString = await taskResponse.Content.ReadAsStringAsync();

        _logger.LogInformation($@"Task requested:
            - response code: {taskResponse.StatusCode}
            - response: {taskResponseString}
        ");
    }

    private async Task<string> GetAccessToken()
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && (_accessTokenExpirationDate == null || _accessTokenExpirationDate > DateTime.UtcNow))
        {
            return _accessToken;
        }

        using var httpClient = new HttpClient { DefaultRequestHeaders = { { "Metadata-Flavor", "Google" } } };

        var accessTokenResponse = await httpClient.GetFromJsonAsync(
            requestUri: $"{Config.GoogleMetadataBaseUrl}/computeMetadata/v1/instance/service-accounts/default/token",
            jsonTypeInfo: SnakeCaseJsonContext.Default.GoogleCloudAccessToken
        );

        if (string.IsNullOrWhiteSpace(accessTokenResponse?.AccessToken))
        {
            _logger.LogError(@$"Google Access Token was unexpectedly null:
                - response: {accessTokenResponse}");

            throw new Exception("Unable to acquire Google Access token");
        }
        
        _accessToken = accessTokenResponse.AccessToken;
        _accessTokenExpirationDate = DateTime.UtcNow.AddSeconds((accessTokenResponse.ExpiresIn ?? 0) - 60);

        return _accessToken;
    }
}
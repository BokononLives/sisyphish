using System.Text;
using System.Text.Json;
using sisyphish.Discord.Models;
using sisyphish.GoogleCloud.Authentication;
using sisyphish.Tools;

namespace sisyphish.GoogleCloud.CloudTasks;

public class CloudTasksService : GoogleCloudService, ICloudTasksService
{
    public CloudTasksService(ILogger<CloudTasksService> logger, IGoogleCloudAuthenticationService authenticationService, HttpClient httpClient): base(logger, authenticationService, httpClient)
    {
    }

    public async Task CreateHttpPostTask(string url, DiscordInteraction body)
    {
        await Authenticate();

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

        var httpResponse = await _httpClient.PostAsJsonAsync(
            requestUri: "tasks",
            value: taskRequest,
            jsonTypeInfo: CamelCaseJsonContext.Default.GoogleCloudTaskRequest
        );

        httpResponse.EnsureSuccessStatusCode();
    }
}
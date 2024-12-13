using System.Text.Json;
using Google.Cloud.Tasks.V2;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace sisyphish.GoogleCloud;

public class CloudTasksService : ICloudTasksService
{
    private readonly CloudTasksClient _cloudTasks;
    private readonly IOptions<JsonOptions> _jsonOptions;
    private readonly ILogger<CloudTasksService> _logger;

    public CloudTasksService(CloudTasksClient cloudTasks, IOptions<JsonOptions> jsonOptions, ILogger<CloudTasksService> logger)
    {
        _cloudTasks = cloudTasks;
        _jsonOptions = jsonOptions;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task CreateHttpPostTask(string url, object body)
    {
        var serializedBody = JsonSerializer.Serialize(body, _jsonOptions.Value.SerializerOptions);

        _logger.LogInformation(serializedBody);

        var createTaskRequest = new CreateTaskRequest
        {
            ParentAsQueueName = QueueName.FromProjectLocationQueue(Config.GoogleProjectId, Config.GoogleLocation, Config.GoogleProjectId),
            Task = new Google.Cloud.Tasks.V2.Task
            {
                HttpRequest = new Google.Cloud.Tasks.V2.HttpRequest
                {
                    HttpMethod = Google.Cloud.Tasks.V2.HttpMethod.Post,
                    Headers = {{ "Content-Type", "application/json" }},
                    Body =  Google.Protobuf.ByteString.CopyFromUtf8(serializedBody),
                    Url = url,
                    OidcToken = new OidcToken
                    {
                        ServiceAccountEmail = Config.GoogleServiceAccount
                    }
                }
            }
        };

        await _cloudTasks.CreateTaskAsync(createTaskRequest);
    }
}
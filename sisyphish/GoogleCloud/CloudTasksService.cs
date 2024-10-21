using System.Text.Json;
using Google.Cloud.Tasks.V2;

namespace sisyphish.GoogleCloud;

public class CloudTasksService : ICloudTasksService
{
    private readonly CloudTasksClient _cloudTasks;

    public CloudTasksService(CloudTasksClient cloudTasks)
    {
        _cloudTasks = cloudTasks;
    }

    public async System.Threading.Tasks.Task CreateHttpPostTask(string url, object body)
    {
        var createTaskRequest = new CreateTaskRequest
        {
            ParentAsQueueName = QueueName.FromProjectLocationQueue(Config.GoogleProjectId, Config.GoogleLocation, Config.GoogleProjectId),
            Task = new Google.Cloud.Tasks.V2.Task
            {
                HttpRequest = new Google.Cloud.Tasks.V2.HttpRequest
                {
                    HttpMethod = Google.Cloud.Tasks.V2.HttpMethod.Post,
                    Headers = {{ "Content-Type", "application/json" }},
                    Body =  Google.Protobuf.ByteString.CopyFromUtf8(JsonSerializer.Serialize(body)),
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
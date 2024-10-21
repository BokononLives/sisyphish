using Google.Cloud.BigQuery.V2;
using Google.Cloud.Tasks.V2;
using Microsoft.AspNetCore.Mvc;

namespace sisyphish.Controllers;

[ApiController]
public class TestController : ControllerBase
{
    private readonly BigQueryClient _bigQueryClient;

    public TestController(BigQueryClient bigQueryClient)
    {
        _bigQueryClient = bigQueryClient;
    }

    [HttpGet("test")]
    public async Task<string> Get()
    {
        var args = new [] { new BigQueryParameter("id", BigQueryDbType.String, "31187caec46b46ee99b04fc751de5e02") };
        var results = await _bigQueryClient.ExecuteQueryAsync("select name from sisyphish.test where id = @id", args);
        var name = results.FirstOrDefault()?["name"];

        var tasksClient = await new CloudTasksClientBuilder().BuildAsync();
        var createTaskRequest = new CreateTaskRequest
        {
            ParentAsQueueName = QueueName.FromProjectLocationQueue(Config.GoogleProjectId, Config.GoogleLocation, Config.GoogleProjectId),
            Task = new Google.Cloud.Tasks.V2.Task
            {
                HttpRequest = new Google.Cloud.Tasks.V2.HttpRequest
                {
                    HttpMethod = Google.Cloud.Tasks.V2.HttpMethod.Get,
                    Url = $"{Config.PublicBaseUrl}/secret",
                    OidcToken = new OidcToken
                    {
                        ServiceAccountEmail = Config.GoogleServiceAccount
                    }
                }
            }
        };

        var createdTask = await tasksClient.CreateTaskAsync(createTaskRequest);

        return $"Looking good, {createdTask.Name} !";
    }
}
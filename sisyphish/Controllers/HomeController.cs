using System.Text;
using System.Text.Json;
using Google.Cloud.Tasks.V2;
using Microsoft.AspNetCore.Mvc;
using sisyphish.Filters;
using sisyphish.Tools.Discord;
using sisyphish.Tools.Discord.Core.Models;
using sisyphish.Tools.Discord.Sisyphish.Models;

namespace sisyphish.Controllers;

[ApiController]
public class HomeController : ControllerBase
{
    private readonly CloudTasksClient _cloudTasks;

    public HomeController(CloudTasksClient cloudTasks)
    {
        _cloudTasks = cloudTasks;
    }

    [HttpGet("")]
    public string Get()
    {
        return "Hello world!";
    }

    [Discord]
    [HttpPost("")]
    public async Task<IActionResult> PostAsync(DiscordInteraction interaction)
    {
        var response = (interaction?.Type) switch
        {
            DiscordInteractionType.Ping => Pong(),
            DiscordInteractionType.ApplicationCommand => await ProcessApplicationCommand(interaction),
            null => new DiscordInteractionErrorResponse { Error = "Interaction type is required" },
            _ => new DiscordInteractionErrorResponse { Error = "Invalid interaction type" },
        };
        
        return this.From(response);
    }

    private static DiscordInteractionResponse Pong()
    {
        return new DiscordInteractionResponse { ContentType = DiscordInteractionResponseContentType.Pong };
    }

    private async Task<IDiscordInteractionResponse> ProcessApplicationCommand(DiscordInteraction interaction)
    {
        return (interaction.Data?.Name) switch
        {
            DiscordCommandName.Fish => await ProcessFishCommand(interaction),
            DiscordCommandName.Reset => await ProcessResetCommand(interaction),
            _ => new DiscordInteractionErrorResponse { Error = "Invalid command name" },
        };
    }

    private async Task<IDiscordInteractionResponse> ProcessFishCommand(DiscordInteraction interaction)
    {
        var response = new DeferredDiscordInteractionResponse();

        var createTaskRequest = new CreateTaskRequest
        {
            ParentAsQueueName = QueueName.FromProjectLocationQueue(Config.GoogleProjectId, Config.GoogleLocation, Config.GoogleProjectId),
            Task = new Google.Cloud.Tasks.V2.Task
            {
                HttpRequest = new Google.Cloud.Tasks.V2.HttpRequest
                {
                    HttpMethod = Google.Cloud.Tasks.V2.HttpMethod.Post,
                    Headers = {{ "Content-Type", "application/json" }},
                    Body =  Google.Protobuf.ByteString.CopyFromUtf8(JsonSerializer.Serialize(interaction)),
                    Url = $"{Config.PublicBaseUrl}/sisyphish/fish",
                    OidcToken = new OidcToken
                    {
                        ServiceAccountEmail = Config.GoogleServiceAccount
                    }
                }
            }
        };

        await _cloudTasks.CreateTaskAsync(createTaskRequest);

        return response;
    }

    private async Task<IDiscordInteractionResponse> ProcessResetCommand(DiscordInteraction interaction)
    {
        var response = new DeferredDiscordInteractionResponse();

        var createTaskRequest = new CreateTaskRequest
        {
            ParentAsQueueName = QueueName.FromProjectLocationQueue(Config.GoogleProjectId, Config.GoogleLocation, Config.GoogleProjectId),
            Task = new Google.Cloud.Tasks.V2.Task
            {
                HttpRequest = new Google.Cloud.Tasks.V2.HttpRequest
                {
                    HttpMethod = Google.Cloud.Tasks.V2.HttpMethod.Post,
                    Headers = {{ "Content-Type", "application/json" }},
                    Body =  Google.Protobuf.ByteString.CopyFromUtf8(JsonSerializer.Serialize(interaction)),
                    Url = $"{Config.PublicBaseUrl}/sisyphish/reset",
                    OidcToken = new OidcToken
                    {
                        ServiceAccountEmail = Config.GoogleServiceAccount
                    }
                }
            }
        };

        await _cloudTasks.CreateTaskAsync(createTaskRequest);

        return response;
    }
}
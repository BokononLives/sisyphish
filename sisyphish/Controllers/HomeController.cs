using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Cloud.BigQuery.V2;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using sisyphish.Filters;
using sisyphish.Tools.Discord;

namespace sisyphish.Controllers;

[ApiController]
[Route("")]
public class HomeController : ControllerBase
{
    // private readonly BigQueryClient _bigQueryClient;

    // public HomeController(BigQueryClient bigQueryClient)
    // {
    //     _bigQueryClient = bigQueryClient;
    // }

    [HttpGet(Name = "")]
    public string Get()
    {
        return $"👋 {Config.DiscordApplicationId}";
    }

    [Discord]
    [HttpPost(Name = "")]
    public async Task<IActionResult> PostAsync(DiscordInteraction interaction)
    {
        switch (interaction?.Type)
        {
            case null:
                return BadRequest();
            case DiscordInteractionType.Ping:
                return Pong();
            case DiscordInteractionType.ApplicationCommand:
                return ProcessApplicationCommand(interaction);
            default:
                return BadRequest();
        }
    }

    private OkObjectResult Pong()
    {
        return Ok(new { Type = DiscordInteractionResponseType.Pong });
    }

    private IActionResult ProcessApplicationCommand(DiscordInteraction interaction)
    {
        switch (interaction.Data?.Name?.ToLower())
        {
            case "fish":
                var response = new //TODO: use DiscordInteractionResponse model, format properly, ignore empty array for flags
                {
                    Type = DiscordInteractionResponseType.ChannelMessageWithSource,
                    Data = new
                    {
                        Content = "Nothing's biting yet..."
                    }
                };
                return new JsonResult(response);
            default:
                return BadRequest();
        }
    }
}
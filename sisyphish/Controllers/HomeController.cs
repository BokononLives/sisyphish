using Microsoft.AspNetCore.Mvc;
using sisyphish.Discord;
using sisyphish.Discord.Models;
using sisyphish.Extensions;
using sisyphish.Filters;
using sisyphish.GoogleCloud;

namespace sisyphish.Controllers;

[ApiController]
public class HomeController : ControllerBase
{
    private readonly ICloudTasksService _cloudTasks;
    private readonly IDiscordService _discord;

    public HomeController(ICloudTasksService cloudTasks, IDiscordService discord)
    {
        _cloudTasks = cloudTasks;
        _discord = discord;
    }

    [HttpGet("")]
    public string Get()
    {
        return "Hello world!";
    }

    [ServiceFilter(typeof(DiscordAttribute))]
    [HttpPost("")]
    public async Task<IActionResult> PostAsync(DiscordInteraction interaction)
    {
        var response = (interaction?.Type) switch
        {
            DiscordInteractionType.Ping => Pong(),
            DiscordInteractionType.ApplicationCommand => await ProcessApplicationCommand(interaction),
            DiscordInteractionType.MessageComponent => await ProcessMessageComponent(interaction),
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
            //DiscordCommandName.Lucky => await ProcessLuckyCommand(interaction),
            _ => new DiscordInteractionErrorResponse { Error = "Invalid command name" },
        };
    }

    private async Task<IDiscordInteractionResponse> ProcessMessageComponent(DiscordInteraction interaction)
    {
        if (interaction.UserId != interaction.PromptUserId)
        {
            return new DiscordInteractionResponse
            {
                ContentType = DiscordInteractionResponseContentType.ChannelMessageWithSource,
                Data = new DiscordInteractionResponseData
                {
                    Flags = DiscordInteractionResponseFlags.Ephemeral,
                    Content = "An unexpected error occurred, please try again later!"
                }
            };
        }

        var response = new DeferredDiscordInteractionResponse();
        
        await _discord.DeferResponse(interaction, isEphemeral: false);
        await _discord.DeleteResponse(interaction, interaction.Message?.Id);
        
        await _cloudTasks.CreateHttpPostTask($"{Config.PublicBaseUrl}/sisyphish/event", interaction);

        return response;
    }

    private async Task<IDiscordInteractionResponse> ProcessFishCommand(DiscordInteraction interaction)
    {
        var eventRoll = Random.Shared.Next(1, 21);
        if (eventRoll == 20)
        {
            interaction.IsLucky = true;
        }
        
        var response = new DeferredDiscordInteractionResponse();
        
        await _discord.DeferResponse(interaction, isEphemeral: interaction.IsLucky == true);
        
        await _cloudTasks.CreateHttpPostTask($"{Config.PublicBaseUrl}/sisyphish/fish", interaction);
        
        return response;
    }

    private async Task<IDiscordInteractionResponse> ProcessResetCommand(DiscordInteraction interaction)
    {
        var response = new DeferredDiscordInteractionResponse();
        
        await _discord.DeferResponse(interaction, isEphemeral: false);
            
        await _cloudTasks.CreateHttpPostTask($"{Config.PublicBaseUrl}/sisyphish/reset", interaction);

        return response;
    }

    // private async Task<IDiscordInteractionResponse> ProcessLuckyCommand(DiscordInteraction interaction)
    // {
    //     interaction.IsLucky = true;
        
    //     var response = new DeferredDiscordInteractionResponse();
        
    //     await _discord.DeferResponse(interaction, isEphemeral: interaction.IsLucky == true);
            
    //     await _cloudTasks.CreateHttpPostTask($"{Config.PublicBaseUrl}/sisyphish/fish", interaction);

    //     return response;
    // }
}
using Microsoft.AspNetCore.Mvc;
using sisyphish.Discord;
using sisyphish.Discord.Models;
using sisyphish.Filters;
using sisyphish.Sisyphish.Processors;

namespace sisyphish.Controllers;

[ApiController]
public class SisyphishController : ControllerBase
{
    private readonly IDiscordService _discord;
    private readonly IEnumerable<ICommandProcessor> _commandProcessors;

    public SisyphishController(IDiscordService discord, IEnumerable<ICommandProcessor> commandProcessors)
    {
        _discord = discord;
        _commandProcessors = commandProcessors;
    }

    [HttpPost("sisyphish/fish")]
    [GoogleCloud]
    public async Task<IActionResult> ProcessFishCommand(DiscordInteraction interaction)
    {
        var commandProcessors = _commandProcessors
            .Where(p => p.Command == DiscordCommandName.Fish)
            .ToList();
        
        await ProcessFollowUpToCommand(interaction, commandProcessors);

        return Ok();
    }

    [HttpPost("sisyphish/event")]
    [GoogleCloud]
    public async Task<IActionResult> ProcessEvent(DiscordInteraction interaction)
    {
        var commandProcessors = _commandProcessors
            .OfType<MessageComponentCommandProcessor>()
            .ToList<ICommandProcessor>();
        
        await ProcessFollowUpToCommand(interaction, commandProcessors);
        
        return Ok();
    }

    [HttpPost("sisyphish/reset")]
    [GoogleCloud]
    public async Task<IActionResult> ProcessResetCommand(DiscordInteraction interaction)
    {
        var commandProcessors = _commandProcessors
            .Where(p => p.Command == DiscordCommandName.Reset)
            .ToList();
        
        await ProcessFollowUpToCommand(interaction, commandProcessors);
        
        return Ok();
    }

    private async Task ProcessFollowUpToCommand(DiscordInteraction interaction, List<ICommandProcessor> processors)
    {
        if (processors.Count != 1)
        {
            await ServeError(interaction, "An unexpected error occurred, please try again later!");
        }

        var commandProcessor = processors.Single();

        await commandProcessor.ProcessFollowUpToCommand(interaction);
    }

    private async Task ServeError(DiscordInteraction interaction, string errorMessage)
    {
        await _discord.EditResponse(interaction, "I sure do love fishin'!", []);
        await _discord.SendFollowupResponse(interaction, errorMessage, [], false);
        await _discord.DeleteResponse(interaction);
    }
}
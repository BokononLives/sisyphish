using Microsoft.AspNetCore.Mvc;
using sisyphish.Filters;
using sisyphish.Tools.Discord;
using sisyphish.Tools.Discord.Models;

namespace sisyphish.Controllers;

[ApiController]
[Route("")]
public class HomeController : ControllerBase
{
    private readonly IDiscordInteractionProcessor _discord;

    public HomeController(IDiscordInteractionProcessor discord)
    {
        _discord = discord;
    }

    [HttpGet(Name = "")]
    public string Get()
    {
        return $"👋 {Config.DiscordApplicationId}";
    }

    [Discord]
    [HttpPost(Name = "")]
    public async Task<IActionResult> PostAsync(DiscordInteraction interaction)
    {
        var response = await _discord.ProcessDiscordInteraction(interaction);
        
        return this.From(response);
    }
}
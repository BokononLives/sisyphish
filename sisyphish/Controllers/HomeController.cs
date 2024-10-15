using Microsoft.AspNetCore.Mvc;
using sisyphish.Filters;
using sisyphish.Tools;

namespace sisyphish.Controllers;

[ApiController]
[Route("")]
public class HomeController : ControllerBase
{
    [HttpGet(Name = "")]
    public string Get()
    {
        return $"👋 {Config.DiscordApplicationId}";
    }

    [Discord]
    [HttpPost(Name = "")]
    public async Task<IActionResult> PostAsync(DiscordInteraction interaction)
    {
        if (interaction == null)
        {
            return BadRequest();
        }

        if (interaction.Type == DiscordInteractionType.Ping)
        {
            return Ok(new { Type = DiscordInteractionResponseType.Pong });
        }

        return Ok();
    }
}
using Microsoft.AspNetCore.Mvc;

namespace sisyphish.Controllers;

[ApiController]
[Route("test")]
public class TestController : ControllerBase
{
    [HttpGet(Name = "test")]
    public string Get()
    {
        return $"Looking good, {Config.DiscordApplicationId} !";
    }
}
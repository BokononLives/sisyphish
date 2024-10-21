using Microsoft.AspNetCore.Mvc;
using sisyphish.Filters;

namespace sisyphish.Controllers;

[ApiController]
public class SecretController : ControllerBase
{
    [HttpGet("secret")]
    [GoogleCloud]
    public async Task<string> Get()
    {
        return await Task.FromResult("You're in!");
    }
}
using Microsoft.AspNetCore.Mvc;
using sisyphish.Filters;

namespace sisyphish.Controllers;

[ApiController]
[Route("secret")]
public class SecretController : ControllerBase
{
    [HttpGet(Name = "secret")]
    [GoogleCloud]
    public async Task<string> Get()
    {
        return await Task.FromResult("You're in!");
    }
}
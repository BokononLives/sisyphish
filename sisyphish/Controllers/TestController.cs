using Google.Cloud.BigQuery.V2;
using Microsoft.AspNetCore.Mvc;

namespace sisyphish.Controllers;

[ApiController]
[Route("test")]
public class TestController : ControllerBase
{

    private readonly BigQueryClient _bigQueryClient;

    public TestController(BigQueryClient bigQueryClient)
    {
        _bigQueryClient = bigQueryClient;
    }

    [HttpGet(Name = "test")]
    public async Task<string> Get()
    {
        var args = new [] { new BigQueryParameter("id", BigQueryDbType.String, "31187caec46b46ee99b04fc751de5e02") };
        var results = await _bigQueryClient.ExecuteQueryAsync("select name from sisyphish.test where id = @id", args);
        var name = results.FirstOrDefault()?["name"];

        return $"Looking good, {name} !";
    }
}
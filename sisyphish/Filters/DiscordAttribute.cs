using System.Net;
using System.Text;
using System.Text.Json;
using Flurl.Http;
using Google.Rpc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using NSec.Cryptography;
using sisyphish.Discord.Models;
using sisyphish.GoogleCloud;

namespace sisyphish.Filters;

public class DiscordAttribute : IAsyncActionFilter
{
    private readonly ICloudTasksService _cloudTasks;
    private readonly ILogger<DiscordAttribute> _logger;

    public DiscordAttribute(ICloudTasksService cloudTasks, ILogger<DiscordAttribute> logger)
    {
        _cloudTasks = cloudTasks;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        context.HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);

        var requestSignature = context.HttpContext.Request.Headers["X-Signature-Ed25519"].FirstOrDefault() ?? string.Empty;
        var requestTimestamp = context.HttpContext.Request.Headers["X-Signature-Timestamp"].FirstOrDefault() ?? string.Empty;
        var requestBody = await new StreamReader(context.HttpContext.Request.Body).ReadToEndAsync();

        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, GetBytesFromHexString(Config.DiscordPublicKey), KeyBlobFormat.RawPublicKey);
        var verified = SignatureAlgorithm.Ed25519.Verify(publicKey, Encoding.UTF8.GetBytes(requestTimestamp + requestBody), GetBytesFromHexString(requestSignature));

        if (!verified)
        {
            context.Result = new UnauthorizedObjectResult("Invalid request");
            return;
        }

        _logger.LogInformation($"Content type = {context.HttpContext.Request.Headers.ContentType}; body = {requestBody}");

        try
        {
            var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerOptions.Default)
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            var interaction = JsonSerializer.Deserialize<DiscordInteraction>(requestBody, jsonSerializerOptions);
            _logger.LogInformation($"interaction = {JsonSerializer.Serialize(interaction)}");
            if (interaction?.Type == DiscordInteractionType.ApplicationCommand)
            {
                switch (interaction.Data?.Name)
                {
                    case DiscordCommandName.Fish:
                        context.HttpContext.Response.OnCompleted(async () =>
                        {
                            await Task.Run(async () =>
                            {
                                _logger.LogInformation($"Logging from within Response.OnCompleted: {context.HttpContext.Response.StatusCode}");
                                if (context.HttpContext.Response.StatusCode == ((int)HttpStatusCode.Accepted))
                                {
                                    try
                                    {
                                        await SendDiscordCallback(interaction);
                                        await _cloudTasks.CreateHttpPostTask($"{Config.PublicBaseUrl}/sisyphish/fish", interaction);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogInformation(ex.ToString());
                                    }
                                }
                            });
                        });
                        break;
                    case DiscordCommandName.Reset:
                        context.HttpContext.Response.OnCompleted(async () =>
                        {
                            _logger.LogInformation("Logging from within Response.OnCompleted");
                            if (context.HttpContext.Response.StatusCode == ((int)HttpStatusCode.Accepted))
                            {
                                await SendDiscordCallback(interaction);
                                await _cloudTasks.CreateHttpPostTask($"{Config.PublicBaseUrl}/sisyphish/reset", interaction);
                            }
                        });
                        break;
                    default:
                        return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.ToString());
        }

        await next();
    }

    private static byte[] GetBytesFromHexString(string hex)
    {
        var length = hex.Length;
        var bytes = new byte[length / 2];

        for (int i = 0; i < length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }

        return bytes;
    }

    private async Task SendDiscordCallback(DiscordInteraction interaction)
    {
        var deferral = new
        {
            type = 5
        };

        var response = await $"{Config.DiscordBaseUrl}/interactions/{interaction.Id}/{interaction.Token}/callback"
            .PostJsonAsync(deferral);

        _logger.LogInformation($"Discord response to callback: {JsonSerializer.Serialize(response)}");
    }
}
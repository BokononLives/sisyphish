using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using sisyphish.Discord.Models;

namespace sisyphish.Discord;

public class DiscordService : IDiscordService
{
    private readonly ILogger<DiscordService> _logger;
    private readonly IOptions<JsonOptions> _jsonOptions;

    public DiscordService(ILogger<DiscordService> logger, IOptions<JsonOptions> jsonOptions)
    {
        _logger = logger;
        _jsonOptions = jsonOptions;
    }

    public async Task DeferResponse(DiscordInteraction interaction, bool isEphemeral)
    {
        var body = new DiscordDeferralCallbackResponse
        {
            IsEphemeral = isEphemeral
        };

        await SendResponse(async (httpClient) =>
        {
            return await httpClient.PostAsJsonAsync(
                requestUri: $"{Config.DiscordBaseUrl}/interactions/{interaction.Id}/{interaction.Token}/callback",
                value: body,
                options: _jsonOptions.Value.JsonSerializerOptions);
        });
    }

    public async Task DeleteResponse(DiscordInteraction interaction, string? messageId = null)
    {
        var messageToDelete = string.IsNullOrWhiteSpace(messageId) ? "@original" : messageId;

        await SendResponse(async (httpClient) =>
        {
            return await httpClient.DeleteAsync(
                requestUri: $"{Config.DiscordBaseUrl}/webhooks/{Config.DiscordApplicationId}/{interaction.Token}/messages/{messageToDelete}"
            );
        });
    }

    public async Task EditResponse(DiscordInteraction interaction, string? content, List<DiscordComponent> components)
    {
        var body = new DiscordInteractionEdit
        {
            Content = content,
            Components = components
        };

        await SendResponse(async (httpClient) =>
        {
            return await httpClient.PatchAsJsonAsync(
                requestUri: $"{Config.DiscordBaseUrl}/webhooks/{Config.DiscordApplicationId}/{interaction.Token}/messages/@original",
                value: body,
                options: _jsonOptions.Value.JsonSerializerOptions
            );
        });
    }

    public async Task SendFollowupResponse(DiscordInteraction interaction, string? content, List<DiscordComponent> components, bool isEphemeral)
    {
        var body = new DiscordInteractionEdit
        {
            Content = content,
            Components = components,
            Flags = isEphemeral ? DiscordInteractionResponseFlags.Ephemeral : null
        };

        await SendResponse(async (httpClient) =>
        {
            return await httpClient.PostAsJsonAsync(
                requestUri: $"{Config.DiscordBaseUrl}/webhooks/{Config.DiscordApplicationId}/{interaction.Token}",
                value: body,
                options: _jsonOptions.Value.JsonSerializerOptions
            );
        });
    }

    private async Task SendResponse(Func<HttpClient, Task<HttpResponseMessage>> sendResponse)
    {
        var success = false;
        var attempts = 0;
        var requestContent = (string?)null;
        var responseErrorContent = string.Empty;
        var responseStatusCode = string.Empty;

        while (!success && attempts < 5)
        {
            attempts++;

            using var httpClient = new HttpClient();
            var response = await sendResponse(httpClient);

            if (response.IsSuccessStatusCode)
            {
                success = true;
            }
            else
            {
                var shouldRetry = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;

                var retryAfter =
                    shouldRetry
                        ? response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1)
                        : TimeSpan.FromSeconds(0);
                
                httpClient.Dispose();

                requestContent = await ((response.RequestMessage?.Content?.ReadAsStringAsync()) ?? Task.FromResult(string.Empty));
                responseErrorContent = await ((response.Content.ReadAsStringAsync()) ?? Task.FromResult(string.Empty));
                responseStatusCode = response.StatusCode.ToString();

                if (attempts >= 5 || !shouldRetry)
                {
                    break;
                }

                _logger.LogError(@$"Failed to respond to interaction - trying again:
                    - status code: {responseStatusCode}
                    - error: {responseErrorContent}
                    - request: {requestContent}".Replace(Environment.NewLine, " "));
                    
                Thread.Sleep(retryAfter);
            }
        }

        if (!success)
        {
            _logger.LogError(@$"Failed to respond to interaction - giving up:
                - status code: {responseStatusCode}
                - error: {responseErrorContent}
                - request: {requestContent}".Replace(Environment.NewLine, " "));
        }
    }
}
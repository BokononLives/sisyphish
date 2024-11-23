using System.Net;
using System.Text.Json;
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

    public async Task DeferResponse(DiscordInteraction interaction)
    {
        var body = new DiscordDeferralCallbackResponse();

        using var httpClient = new HttpClient();
        await httpClient.PostAsJsonAsync(
            requestUri: $"{Config.DiscordBaseUrl}/interactions/{interaction.Id}/{interaction.Token}/callback",
            value: body,
            options: _jsonOptions.Value.JsonSerializerOptions);
    }

    public async Task EditResponse(DiscordInteraction interaction, string? content, List<DiscordComponent> components)
    {
        var body = new DiscordInteractionEdit
        {
            Content = content,
            Components = components
        };

        var success = false;
        var attempts = 0;
        var requestContent = string.Empty;
        var responseErrorContent = string.Empty;

        while (!success && attempts < 5)
        {
            attempts++;

            using var httpClient = new HttpClient();
            var response = await httpClient.PatchAsJsonAsync(
                requestUri: $"{Config.DiscordBaseUrl}/webhooks/{Config.DiscordApplicationId}/{interaction.Token}/messages/@original",
                value: body,
                options: _jsonOptions.Value.JsonSerializerOptions
            );

            if (response.StatusCode == HttpStatusCode.OK)
            {
                success = true;
            }
            else
            {
                httpClient.Dispose();

                if (attempts == 5)
                {
                    requestContent = await response.RequestMessage!.Content!.ReadAsStringAsync();
                    responseErrorContent = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Thread.Sleep(1_000);
                }
            }
        }

        if (!success)
        {
            _logger.LogError(@$"Failed to respond to interaction: {JsonSerializer.Serialize(interaction)}
                - with: {content}
                - error: {responseErrorContent}
                - request: {requestContent}".Replace(Environment.NewLine, " "));
        }
    }
}
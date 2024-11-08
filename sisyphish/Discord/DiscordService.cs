using System.Net;
using System.Text.Json;
using sisyphish.Discord.Models;

namespace sisyphish.Discord;

public class DiscordService : IDiscordService
{
    private readonly ILogger<DiscordService> _logger;

    public DiscordService(ILogger<DiscordService> logger)
    {
        _logger = logger;
    }

    public async Task DeferResponse(DiscordInteraction interaction)
    {
        var body = new DiscordDeferralCallbackResponse();

        using var httpClient = new HttpClient();
        await httpClient.PostAsJsonAsync(
            requestUri: $"{Config.DiscordBaseUrl}/interactions/{interaction.Id}/{interaction.Token}/callback",
            value: body);
    }

    public async Task EditResponse(DiscordInteraction interaction, string content, List<DiscordComponent> components)
    {
        var body = new DiscordInteractionEdit
        {
            Content = content,
            Components = components
        };

        var success = false;
        var attempts = 0;
        while (!success && attempts < 5)
        {
            attempts++;

            using var httpClient = new HttpClient();
            var response = await httpClient.PatchAsJsonAsync(
                requestUri: $"{Config.DiscordBaseUrl}/webhooks/{Config.DiscordApplicationId}/{interaction.Token}/messages/@original",
                value: body
            );

            if (response.StatusCode == HttpStatusCode.OK)
            {
                success = true;
            }
            else
            {
                httpClient.Dispose();
                Thread.Sleep(1_000);
            }
        }

        if (!success)
        {
            _logger.LogError($"Failed to respond to interaction: {JsonSerializer.Serialize(interaction)} - with: {content}");
        }
    }
}
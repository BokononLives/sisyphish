using System.Net;
using Flurl.Http;
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
        var deferral = new DiscordDeferralCallbackResponse();

        using var httpClient = new HttpClient();
        await httpClient.PostAsJsonAsync(
            requestUri: $"{Config.DiscordBaseUrl}/interactions/{interaction.Id}/{interaction.Token}/callback",
            value: deferral);
    }

    public async Task EditResponse(DiscordInteraction interaction, string content)
    {
        var body = new DiscordInteractionEdit
        {
            Content = content
        };

        var success = false;
        var attempts = 0;
        while (!success && attempts < 5)
        {
            attempts++;
            
            var response = await $"{Config.DiscordBaseUrl}/webhooks/{Config.DiscordApplicationId}/{interaction.Token}/messages/@original"
                .AllowAnyHttpStatus()
                .PatchJsonAsync(body);
                
            if (response.StatusCode == (int)HttpStatusCode.OK)
            {
                success = true;
            }
        }
    }
}
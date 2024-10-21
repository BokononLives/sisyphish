using Flurl.Http;
using Google.Cloud.BigQuery.V2;
using Microsoft.AspNetCore.Mvc;
using sisyphish.Discord.Models;
using sisyphish.Filters;
using sisyphish.Sisyphish.Models;

namespace sisyphish.Controllers;

[ApiController]
public class SisyphishController : ControllerBase
{
    private readonly BigQueryClient _bigQueryClient;

    public SisyphishController(BigQueryClient bigQueryClient)
    {
        _bigQueryClient = bigQueryClient;
    }

    [HttpPost("sisyphish/fish")]
    [GoogleCloud]
    public async Task<IActionResult> ProcessFishCommand(DiscordInteraction interaction)
    {
        var fisher = await GetOrCreateFisher(interaction);

        var response = new DiscordInteractionEdit
        {
            Content = $"Nothing's biting yet, <@{interaction.UserId}>... [Fish caught: {fisher?.FishCaught ?? 0} / Biggest fish: {fisher?.BiggestFish ?? 0} cm]"
        };

        await $"{Config.DiscordBaseUrl}/webhooks/{Config.DiscordApplicationId}/{interaction.Token}/messages/@original"
            .PatchJsonAsync(response);

        return Ok();
    }

    [HttpPost("sisyphish/reset")]
    [GoogleCloud]
    public async Task<IActionResult> ProcessResetCommand(DiscordInteraction interaction)
    {
        await DeleteFisher(interaction);

        var response = new DiscordInteractionEdit
        {
            Content = $"Bye, <@{interaction.UserId}>!"
        };

        await $"{Config.DiscordBaseUrl}/webhooks/{Config.DiscordApplicationId}/{interaction.Token}/messages/@original"
            .PatchJsonAsync(response);

        return Ok();
    }

    private async Task<Fisher?> GetOrCreateFisher(DiscordInteraction interaction)
    {
        var fisher =
               (await GetFisher(interaction))
            ?? (await CreateFisher(interaction));
        
        return fisher;
    }

    private async Task<Fisher?> GetFisher(DiscordInteraction interaction)
    {
        var args = new [] { new BigQueryParameter("discord_user_id", BigQueryDbType.String, interaction.UserId) };
        var rows = await _bigQueryClient.ExecuteQueryAsync("select * from sisyphish.fishers where discord_user_id = @discord_user_id", args);
        var row = rows.FirstOrDefault();

        if (row == null)
        {
            return null;
        }

        var fisher = new Fisher
        {
            Id = row["id"].ToString(),
            CreatedAt = DateTime.Parse(row["created_at"].ToString()!),
            DiscordUserId = row["discord_user_id"].ToString(),
            FishCaught = long.Parse(row["fish_caught"].ToString()!),
            BiggestFish = long.Parse(row["biggest_fish"].ToString()!)
        };

        return fisher;
    }

    private async Task<Fisher?> CreateFisher(DiscordInteraction interaction)
    {
        var fisher = new Fisher
        {
            Id = Guid.NewGuid().ToString().Replace("-", string.Empty),
            CreatedAt = DateTime.UtcNow,
            DiscordUserId = interaction.UserId,
            FishCaught = 0,
            BiggestFish = 0
        };

        var args = new []
        {
            new BigQueryParameter("id", BigQueryDbType.String, fisher.Id),
            new BigQueryParameter("created_at", BigQueryDbType.Timestamp, fisher.CreatedAt),
            new BigQueryParameter("discord_user_id", BigQueryDbType.String, fisher.DiscordUserId),
            new BigQueryParameter("fish_caught", BigQueryDbType.Int64, fisher.FishCaught),
            new BigQueryParameter("biggest_fish", BigQueryDbType.Int64, fisher.BiggestFish)
        };

        await _bigQueryClient.ExecuteQueryAsync(@"
            insert into sisyphish.fishers
                (id, created_at, discord_user_id, fish_caught, biggest_fish)
            values
                (@id, @created_at, @discord_user_id, @fish_caught, @biggest_fish)", args);
        
        return fisher;
    }

    private async Task DeleteFisher(DiscordInteraction interaction)
    {
        var args = new [] { new BigQueryParameter("discord_user_id", BigQueryDbType.String, interaction.UserId) };
        await _bigQueryClient.ExecuteQueryAsync("delete from sisyphish.fishers where discord_user_id = @discord_user_id", args);
    }
}
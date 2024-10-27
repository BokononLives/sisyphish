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

        var expedition = GoFish();

        if (expedition.CaughtFish == true)
        {
            fisher!.FishCaught += 1;
            fisher.BiggestFish = Math.Max(fisher.BiggestFish!.Value, expedition.FishSize!.Value);
        }

        var content = expedition.ToString(fisher!);
        //TODO: be more efficient
            //is Pub/Sub faster than Cloud Tasks?
        //TODO: handle rapid or concurrent requests. Only allow one per user at a time, or update fish count in realtime.
            //multiple dependendent tasks?
            //table of fish?
        //account for "user deleted message" error - distinguish between "message doesn't exist yet" timing issue?
            //switch to 202 and Callback style?
        //TODO: Google BigQuery jobless for low latency queries?
            //switch to Firestore or Postgres or whatever if necessary

        var response = new DiscordInteractionEdit
        {
            Content = content
        };
        
        await $"{Config.DiscordBaseUrl}/webhooks/{Config.DiscordApplicationId}/{interaction.Token}/messages/@original"
            .PatchJsonAsync(response);
        
        if (expedition.CaughtFish == true)
        {
            await AddFish(interaction, fisher!);
        }

        return Ok();
    }

    private static Expedition GoFish()
    {
        var expedition = new Expedition
        {
            FishSize = null,
            CaughtFish = false
        };

        var biteRoll = Random.Shared.Next(1, 10);
        if (biteRoll <= 4)
        {
            return expedition;
        }

        var fishSize = 0;
        int fishRoll;

        do
        {
            fishRoll = Random.Shared.Next(1, 11);
            fishSize += fishRoll;
        } while (fishRoll == 10);

        expedition.FishSize = fishSize;

        var reelStrength = 2;
        int reelRoll;

        do
        {
            reelRoll = Random.Shared.Next(1, 10);
            reelStrength += reelRoll;
        } while (reelRoll == 10);

        if (reelStrength >= fishSize)
        {
            expedition.CaughtFish = true;
        }

        return expedition;
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

    private async Task AddFish(DiscordInteraction interaction, Fisher fisher)
    {
        var args = new []
        {
            new BigQueryParameter("discord_user_id", BigQueryDbType.String, interaction.UserId),
            new BigQueryParameter("fish_caught", BigQueryDbType.Int64, fisher.FishCaught),
            new BigQueryParameter("biggest_fish", BigQueryDbType.Int64, fisher.BiggestFish)
        };

        await _bigQueryClient.ExecuteQueryAsync(@"
            update sisyphish.fishers
            set fish_caught = @fish_caught,
            biggest_fish = @biggest_fish
            where discord_user_id = @discord_user_id", args);
    }

    private async Task DeleteFisher(DiscordInteraction interaction)
    {
        var args = new [] { new BigQueryParameter("discord_user_id", BigQueryDbType.String, interaction.UserId) };
        await _bigQueryClient.ExecuteQueryAsync("delete from sisyphish.fishers where discord_user_id = @discord_user_id", args);
    }
}
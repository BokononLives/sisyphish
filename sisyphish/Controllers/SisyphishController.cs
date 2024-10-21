using System.Text;
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

        var content = new StringBuilder();
        content.AppendLine($"You cast your line into the Sea of Possibilities...");

        var biteRoll = Random.Shared.Next(1, 10);

        if (biteRoll <= 4)
        {
            content.AppendLine("...But nothing's biting!");
        }
        else
        {
            var fishSize = 0;
            int fishRoll;

            do
            {
                fishRoll = Random.Shared.Next(1, 10);
                fishSize += fishRoll;
            } while (fishRoll == 10);

            if (fishRoll <= 2)
            {
                content.AppendLine("You feel the smallest nibble...");
            }
            else if (fishRoll <= 5)
            {
                content.AppendLine("Something's biting!");
            }
            else if (fishRoll <= 20)
            {
                content.AppendLine("Something's biting, and it's a pretty big one!");
            }
            else if (fishRoll <= 50)
            {
                content.AppendLine("Something's biting, and it's a HUGE one!");
            }
            else
            {
                content.AppendLine("It's a massive trophy fish! Don't let this one get away!");
            }

            var reelStrength = 2;
            int reelRoll;

            do
            {
                reelRoll = Random.Shared.Next(1, 10);
                reelStrength += reelRoll;
            } while (reelRoll == 10);

            if (reelStrength < fishSize)
            {
                content.AppendLine("It got away...");
            }
            else
            {
                content.AppendLine($"You reel it in! Congratulations! You got a fish. It's {fishSize} cm!");
                
                if (fishSize > fisher!.BiggestFish)
                {
                    content.AppendLine($"A new personal best!");
                }
                else
                {
                    content.AppendLine($"(Biggest fish caught so far: {fisher.BiggestFish} cm)");
                }

                fisher.FishCaught += 1;
                fisher.BiggestFish = Math.Max(fisher.BiggestFish!.Value, fishSize);

                await AddFish(interaction, fisher);
                

                content.AppendLine($"Fish caught by <@{interaction.UserId}>: {fisher.FishCaught}");
            }
        }

        var response = new DiscordInteractionEdit
        {
            Content = content.ToString()
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
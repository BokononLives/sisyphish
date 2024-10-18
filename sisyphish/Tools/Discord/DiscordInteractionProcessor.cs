using Google.Cloud.BigQuery.V2;
using sisyphish.Tools.Discord.Core.Models;
using sisyphish.Tools.Discord.Sisyphish.Models;

namespace sisyphish.Tools.Discord;

public class DiscordInteractionProcessor : IDiscordInteractionProcessor
{
    private readonly BigQueryClient _bigQueryClient;

    public DiscordInteractionProcessor(BigQueryClient bigQueryClient)
    {
        _bigQueryClient = bigQueryClient;
    }

    public async Task<IDiscordInteractionResponse> ProcessDiscordInteraction(DiscordInteraction interaction)
    {
        return (interaction?.Type) switch
        {
            DiscordInteractionType.Ping => Pong(),
            DiscordInteractionType.ApplicationCommand => await ProcessApplicationCommand(interaction),
            null => new DiscordInteractionErrorResponse { Error = "Interaction type is required" },
            _ => new DiscordInteractionErrorResponse { Error = "Invalid interaction type" },
        };
    }

    private static DiscordInteractionResponse Pong()
    {
        return new DiscordInteractionResponse { ContentType = DiscordInteractionResponseContentType.Pong };
    }

    private async Task<IDiscordInteractionResponse> ProcessApplicationCommand(DiscordInteraction interaction)
    {
        return (interaction.Data?.Name) switch
        {
            DiscordCommandName.Fish => await ProcessFishCommand(interaction),
            DiscordCommandName.Reset => await ProcessResetCommand(interaction),
            _ => new DiscordInteractionErrorResponse { Error = "Invalid command name" },
        };
    }

    private async Task<IDiscordInteractionResponse> ProcessFishCommand(DiscordInteraction interaction)
    {
        var fisher = await GetOrCreateFisher(interaction);

        return new DiscordInteractionResponse
        {
            ContentType = DiscordInteractionResponseContentType.ChannelMessageWithSource,
            Data = new DiscordInteractionResponseData
            {
                Content = $"Nothing's biting yet, <@{interaction.UserId}>... [Fish caught: {fisher?.FishCaught ?? 0} / Biggest fish: {fisher?.BiggestFish ?? 0} cm]"
            }
        };
    }

    private async Task<IDiscordInteractionResponse> ProcessResetCommand(DiscordInteraction interaction)
    {
        await DeleteFisher(interaction);

        return new DiscordInteractionResponse
        {
            ContentType = DiscordInteractionResponseContentType.ChannelMessageWithSource,
            Data = new DiscordInteractionResponseData
            {
                Content = $"Bye, <@{interaction.UserId}>!"
            }
        };
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
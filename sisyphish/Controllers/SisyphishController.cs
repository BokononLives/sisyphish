using System.Text.Json;
using Flurl.Http;
using Google.Cloud.Firestore;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using sisyphish.Discord.Models;
using sisyphish.Filters;
using sisyphish.Sisyphish.Models;

namespace sisyphish.Controllers;

[ApiController]
public class SisyphishController : ControllerBase
{
    private readonly FirestoreDb _firestoreDb;
    private readonly ILogger<SisyphishController> _logger;

    public SisyphishController(FirestoreDb firestoreDb, ILogger<SisyphishController> logger)
    {
        _firestoreDb = firestoreDb;
        _logger = logger;
    }

    [HttpPost("sisyphish/fish")]
    [GoogleCloud]
    public async Task<IActionResult> ProcessFishCommand(DiscordInteraction interaction)
    {
        _logger.LogInformation("Go fish");

        var fisher = await GetFisher(interaction) ?? await CreateFisher(interaction);
        if (fisher == null)
        {
            await $"{Config.DiscordBaseUrl}/webhooks/{Config.DiscordApplicationId}/{interaction.Token}/messages/@original"
                .PatchJsonAsync(new DiscordInteractionEdit
                {
                    Content = $"An unexpected error occurred, please try again later!"
                });

            return Ok();
        }

        if (fisher.LockedAt != null && fisher.LockedAt > DateTime.UtcNow.AddMinutes(-1))
        {
            await $"{Config.DiscordBaseUrl}/webhooks/{Config.DiscordApplicationId}/{interaction.Token}/messages/@original"
                .PatchJsonAsync(new DiscordInteractionEdit
                {
                    Content = $"<@{interaction.UserId}>, you are sending messages too quickly, please try again in a moment!"
                });

            return Ok();
        }

        try
        {
            await LockFisher(fisher);
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.FailedPrecondition)
        {
            await $"{Config.DiscordBaseUrl}/webhooks/{Config.DiscordApplicationId}/{interaction.Token}/messages/@original"
                .PatchJsonAsync(new DiscordInteractionEdit
                {
                    Content = $"<@{interaction.UserId}>, you are sending messages too quickly, please try again in a moment!"
                });

            return Ok();
        }

        var expedition = GoFish();

        var content = expedition.ToString(fisher!);

        //TODO:
            //should we go back to a fish count instead of an array of fish? (yes)
            //account for "user deleted message" error - distinguish between "message doesn't exist yet" timing issue? (yes)
            //multiple dependendent tasks? (eh)
            //is Pub/Sub faster than Cloud Tasks? (eh)

        await $"{Config.DiscordBaseUrl}/webhooks/{Config.DiscordApplicationId}/{interaction.Token}/messages/@original"
            .PatchJsonAsync(new DiscordInteractionEdit
            {
                Content = content
            });

        if (expedition.CaughtFish == true)
        {
            await AddFish(fisher!, (long)expedition!.FishSize!);
        }

        await UnlockFisher(fisher);

        return Ok();
    }

    private async Task<Fisher?> GetFisher(DiscordInteraction interaction)
    {
        var documents = await _firestoreDb.Collection("fishers")
            .WhereEqualTo("discord_user_id", interaction.UserId)
            .Limit(1)
            .GetSnapshotAsync();

        var document = documents.SingleOrDefault();
        if (document == null)
        {
            return null;
        }

        var fisher = document.ConvertTo<Fisher>();
        fisher.Id = document.Id;
        fisher.LastUpdated = document.UpdateTime?.ToDateTime();
        return fisher;
    }

    private async Task LockFisher(Fisher fisher)
    {
        await _firestoreDb.Collection("fishers")
            .Document(fisher.Id)
            .UpdateAsync("locked_at", DateTime.UtcNow, Precondition.LastUpdated(Timestamp.FromDateTime(fisher.LastUpdated!.Value)));
    }

    private async Task UnlockFisher(Fisher fisher)
    {
        await _firestoreDb.Collection("fishers")
            .Document(fisher.Id)
            .UpdateAsync("locked_at", null);
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

    private async Task<Fisher?> CreateFisher(DiscordInteraction interaction)
    {
        var fisher = new Fisher
        {
            CreatedAt = DateTime.UtcNow,
            DiscordUserId = interaction.UserId,
            FishCaught = 0,
            BiggestFish = 0
        };

        var docRef = await _firestoreDb.Collection("fishers").AddAsync(fisher);
        var document = await docRef.GetSnapshotAsync();

        fisher.Id = document.Id;
        fisher.LastUpdated = document.UpdateTime?.ToDateTime();

        return fisher;
    }

    private async Task AddFish(Fisher fisher, long fishSize)
    {
        await _firestoreDb.Collection("fishers")
            .Document(fisher.Id)
            .UpdateAsync(new Dictionary<string, object>
            {
                { "fish_caught", FieldValue.Increment(1) },
                { "biggest_fish", Math.Max(fisher.BiggestFish ?? 0, fishSize) }
            });
    }

    private async Task DeleteFisher(DiscordInteraction interaction)
    {
        var documents = await _firestoreDb.Collection("fishers")
            .WhereEqualTo("discord_user_id", interaction.UserId)
            .Limit(1)
            .GetSnapshotAsync();

        var document = documents.SingleOrDefault();

        if (document != null)
        {
            await document.Reference.DeleteAsync();
        }
    }
}
using System.Text.Json;
using Flurl.Http;
using Google.Cloud.Firestore;
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
        
        var fisher = await GetOrCreateFisher(interaction);

        var expedition = GoFish();

        if (expedition.CaughtFish == true)
        {
            fisher!.Fish.Add(new Dictionary<string, object> { {"type", "betta_tester"}, {"size", expedition.FishSize! }});
        }

        var content = expedition.ToString(fisher!);
        //TODO: be more efficient
            //is Pub/Sub faster than Cloud Tasks?
            //should we go back to a fish count instead of an array of fish?
        //TODO: handle rapid or concurrent requests
            //only allow one per user at a time?
            //multiple dependendent tasks?
        //account for "user deleted message" error - distinguish between "message doesn't exist yet" timing issue?

        var response = new DiscordInteractionEdit
        {
            Content = content
        };
        
        await $"{Config.DiscordBaseUrl}/webhooks/{Config.DiscordApplicationId}/{interaction.Token}/messages/@original"
            .PatchJsonAsync(response);
        
        if (expedition.CaughtFish == true)
        {
            await AddFish(fisher!, new Fish { Type = "betta_tester", Size = expedition.FishSize });
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
        var documents = await _firestoreDb.Collection("fishers")
            .WhereEqualTo("discord_user_id", interaction.UserId)
            .Limit(1)
            .GetSnapshotAsync();
        var document = documents.SingleOrDefault();

        if (document == null)
        {
            return null;
        }

        var fields = document.ToDictionary();
        
        var firestoreFisher = new Fisher
        {
            Id = document.Id,
            CreatedAt = ((Timestamp)fields["created_at"]).ToDateTime(),
            DiscordUserId = (string)fields["discord_user_id"],
            Fish = ((List<object>)fields["fish_caught"]).OfType<Dictionary<string,object>>().ToList()
        };

        _logger.LogInformation($"Firestore fisher found! {JsonSerializer.Serialize(firestoreFisher)}");

        return firestoreFisher;
    }

    private async Task<Fisher?> CreateFisher(DiscordInteraction interaction)
    {
        var fisher = new Fisher
        {
            CreatedAt = DateTime.UtcNow,
            DiscordUserId = interaction.UserId,
            Fish = []
        };

        var document = await _firestoreDb.Collection("fishers").AddAsync(fisher);
        fisher.Id = document.Id;
        
        return fisher;
    }

    private async Task AddFish(Fisher fisher, Fish fish)
    {
        await _firestoreDb.Collection("fishers")
            .Document(fisher.Id)
            .UpdateAsync("fish_caught", FieldValue.ArrayUnion(
                new Dictionary<string, object>{
                    { "type", fish.Type! },
                    { "size", fish.Size! }
                }));
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
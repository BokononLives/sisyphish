using Google.Cloud.Firestore;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using sisyphish.Discord;
using sisyphish.Discord.Models;
using sisyphish.Filters;
using sisyphish.Sisyphish.Models;

namespace sisyphish.Controllers;

[ApiController]
public class SisyphishController : ControllerBase
{
    private readonly FirestoreDb _firestoreDb;
    private readonly IDiscordService _discord;
    private readonly ILogger<SisyphishController> _logger;

    public SisyphishController(FirestoreDb firestoreDb, IDiscordService discord, ILogger<SisyphishController> logger)
    {
        _firestoreDb = firestoreDb;
        _discord = discord;
        _logger = logger;
    }

    [HttpPost("sisyphish/fish")]
    [GoogleCloud]
    public async Task<IActionResult> ProcessFishCommand(DiscordInteraction interaction)
    {
        var initFisherResult = await InitFisher(interaction);
        var fisher = initFisherResult?.Fisher;

        var expedition = initFisherResult?.InitSuccess == true ? GoFish(fisher) : null;

        await UpdateFisher(interaction, fisher, expedition);
        await UpdateDiscord(interaction, initFisherResult, expedition);
              
        await UnlockFisher(fisher);

        return Ok();
    }

    [HttpPost("sisyphish/reset")]
    [GoogleCloud]
    public async Task<IActionResult> ProcessResetCommand(DiscordInteraction interaction)
    {
        await DeleteFisher(interaction);
        
        var content = $"Bye, <@{interaction.UserId}>!";
        await _discord.EditResponse(interaction, content, []);
        
        return Ok();
    }

    private async Task<InitFisherResult?> InitFisher(DiscordInteraction interaction)
    {
        try
        {
            var result = new InitFisherResult();

            var fisher = await GetFisher(interaction) ?? await CreateFisher(interaction);
            result.Fisher = fisher;

            if (fisher == null)
            {
                _logger.LogWarning($"Fisher was unexpectedly null - {interaction.UserId}");
            }
            else if (fisher.IsLocked)
            {
                _logger.LogInformation($"Fisher was locked - {interaction.UserId}");
            }
            else
            {
                try
                {
                    await LockFisher(fisher);
                    result.InitSuccess = true;
                }
                catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.FailedPrecondition)
                {
                    _logger.LogInformation($"Failed to lock fisher - {interaction.UserId}");
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error initializing fisher");

            return new InitFisherResult();
        }
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

    private async Task CreatePrompt(DiscordInteraction interaction, Expedition expedition)
    {
        var prompt = new Prompt
        {
            CreatedAt = DateTime.UtcNow,
            DiscordUserId = interaction.UserId,
            DiscordPromptId = expedition.PromptId,
            Event = expedition.Event
        };

        await _firestoreDb.Collection("prompts").AddAsync(prompt);
    }

    private async Task LockFisher(Fisher fisher)
    {
        await _firestoreDb.Collection("fishers")
            .Document(fisher.Id)
            .UpdateAsync("locked_at", DateTime.UtcNow, Precondition.LastUpdated(Timestamp.FromDateTime(fisher.LastUpdated!.Value)));
    }

    private async Task UnlockFisher(Fisher? fisher)
    {
        try
        {
            if (fisher == null)
            {
                return;
            }
            
            await _firestoreDb.Collection("fishers")
                .Document(fisher.Id)
                .UpdateAsync("locked_at", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error unlocking fisher");
        }
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

    private static Expedition? GoFish(Fisher? fisher)
    {
        if (fisher == null)
        {
            return null;
        }

        var expedition = new Expedition
        {
            Event = Event.None,
            FishSize = null,
            CaughtFish = false
        };

        var eventRoll = Random.Shared.Next(1, 21);
        if (eventRoll == 20)
        {
            expedition.Event = Event.FoundTreasureChest;
            return expedition;
        }

        var biteRoll = Random.Shared.Next(1, 11);
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

    private async Task UpdateFisher(DiscordInteraction? interaction, Fisher? fisher, Expedition? expedition)
    {
        try
        {
            if (interaction == null || fisher == null || expedition == null)
            {
                return;
            }

            switch (expedition.Event)
            {
                case Event.FoundTreasureChest:
                    await CreatePrompt(interaction, expedition);
                    break;
                default:
                    break;
            }
            
            if (expedition.CaughtFish == true)
            {
                await AddFish(fisher, (long)expedition.FishSize!);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating fisher");
        }
    }

    private async Task UpdateDiscord(DiscordInteraction interaction, InitFisherResult? initFisherResult, Expedition? expedition)
    {
        try
        {
            var content = GetDiscordContent(initFisherResult, expedition);
            var components = GetDiscordComponents(initFisherResult, expedition);

            await _discord.EditResponse(interaction, content!, components);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating Discord");
        }
    }

    private static string? GetDiscordContent(InitFisherResult? initFisherResult, Expedition? expedition)
    {
        if (initFisherResult?.Fisher == null)
        {
            return "An unexpected error occurred, please try again later!";
        }

        if (!initFisherResult.InitSuccess)
        {
            return $"<@{initFisherResult.Fisher.DiscordUserId}>, you are sending messages too quickly, please try again in a moment!";
        }

        if (expedition == null)
        {
            return "An unexpected error occurred, please try again later!";
        }

        var content = expedition.GetContent(initFisherResult.Fisher);
        return content;
    }

    private static List<DiscordComponent> GetDiscordComponents(InitFisherResult? initFisherResult, Expedition? expedition)
    {
        if (initFisherResult?.Fisher == null || expedition == null)
        {
            return [];
        }

        var components = expedition.GetComponents();
        return components;
    }
}
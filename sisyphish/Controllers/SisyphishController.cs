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

        var expedition =
              initFisherResult?.InitSuccess != true ? null
            : interaction.IsLucky == true ? GetLucky(fisher)
            : GoFish(fisher);

        await UpdateFisher(interaction, fisher, expedition);
        await UpdateDiscord(interaction, initFisherResult, expedition);
        
        await UnlockFisher(fisher);

        return Ok();
    }

    [HttpPost("sisyphish/event")]
    [GoogleCloud]
    public async Task<IActionResult> ProcessEvent(DiscordInteraction interaction)
    {
        throw new NotImplementedException();

        // var initPromptResult = await InitPrompt(interaction);
        // var prompt = initPromptResult?.Prompt;

        //TODO:

        //NEW COMMENTS:
        // save interaction id and token in prompts table so we can follow up?


        //OLD COMMENTS:
        /*
         * do we want to / can we send the button as an ephemeral follow up to the original message?
         * if so:
         *      what should the original message say?
         * if not:
         *      can we have the button safely "do nothing" if the wrong user clicks it?
         *      let's edit the message when the prompt resolves?
         */   
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

    private async Task<InitPromptResult?> InitPrompt(DiscordInteraction interaction)
    {
        try
        {
            var result = new InitPromptResult();

            var prompt = await GetPrompt(interaction);
            result.Prompt = prompt;

            if (prompt == null)
            {
                _logger.LogWarning($"Prompt was unexpectedly null - {interaction.Data?.CustomId}");
            }
            else if (prompt.IsLocked)
            {
                _logger.LogInformation($"Prompt was locked - {interaction.PromptId}");
            }
            else
            {
                try
                {
                    await LockPrompt(prompt);
                    result.InitSuccess = true;
                }
                catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.FailedPrecondition)
                {
                    _logger.LogInformation($"Failed to lock prompt - {interaction.PromptId}");
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error initializing prompt");

            return new InitPromptResult();
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

    private async Task<Prompt?> GetPrompt(DiscordInteraction interaction)
    {
        if (string.IsNullOrWhiteSpace(interaction.PromptId))
        {
            return null;
        }

        var documents = await _firestoreDb.Collection("prompts")
            .WhereEqualTo("discord_user_id", interaction.UserId)
            .WhereEqualTo("discord_prompt_id", interaction.PromptId)
            .Limit(1)
            .GetSnapshotAsync();

        var document = documents.SingleOrDefault();
        if (document == null)
        {
            return null;
        }

        var prompt = document.ConvertTo<Prompt>();
        prompt.Id = document.Id;
        prompt.LastUpdated = document.UpdateTime?.ToDateTime();
        return prompt;
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

    private async Task LockPrompt(Prompt prompt)
    {
        await _firestoreDb.Collection("prompts")
            .Document(prompt.Id)
            .UpdateAsync("locked_at", DateTime.UtcNow, Precondition.LastUpdated(Timestamp.FromDateTime(prompt.LastUpdated!.Value)));
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
    
    private static Expedition? GetLucky(Fisher? fisher)
    {
        var expedition = new Expedition
        {
            Event = Event.None,
            FishSize = null,
            CaughtFish = false
        };

        expedition.Event = Event.FoundTreasureChest;
        return expedition;
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
            if (initFisherResult?.Fisher == null)
            {
                await ServeError(interaction, "An unexpected error occurred, please try again later!");
            }
            else if (!initFisherResult.InitSuccess)
            {
                await ServeError(interaction, $"<@{initFisherResult.Fisher.DiscordUserId}>, you are sending messages too quickly, please try again in a moment!");
            }
            else if (expedition == null)
            {
                await ServeError(interaction, "An unexpected error occurred, please try again later!");
            }
            else
            {
                var content = expedition.GetContent(initFisherResult.Fisher);
                var components = GetDiscordComponents(initFisherResult, expedition);

                await _discord.EditResponse(interaction, content, components);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating Discord");
        }
    }

    private async Task ServeError(DiscordInteraction interaction, string errorMessage)
    {
        await _discord.EditResponse(interaction, "I sure do love fishin'!", []);
        await _discord.SendFollowupResponse(interaction, errorMessage, [], false);
        await _discord.DeleteResponse(interaction);
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
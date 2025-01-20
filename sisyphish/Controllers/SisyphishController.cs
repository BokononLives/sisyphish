using Google.Cloud.Firestore;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using sisyphish.Discord;
using sisyphish.Discord.Models;
using sisyphish.Filters;
using sisyphish.Sisyphish.Models;
using sisyphish.Sisyphish.Processors;

namespace sisyphish.Controllers;

[ApiController]
public class SisyphishController : ControllerBase
{
    private readonly FirestoreDb _firestoreDb;
    private readonly IDiscordService _discord;
    private readonly ILogger<SisyphishController> _logger;
    private readonly IEnumerable<ICommandProcessor> _commandProcessors;

    public SisyphishController(FirestoreDb firestoreDb, IDiscordService discord, ILogger<SisyphishController> logger, IEnumerable<ICommandProcessor> commandProcessors)
    {
        _firestoreDb = firestoreDb;
        _discord = discord;
        _logger = logger;
        _commandProcessors = commandProcessors;
    }

    [HttpPost("sisyphish/fish")]
    [GoogleCloud]
    public async Task<IActionResult> ProcessFishCommand(DiscordInteraction interaction)
    {
        var commandProcessors = _commandProcessors
            .Where(p => p.Command == DiscordCommandName.Fish)
            .ToList();
        
        await ProcessFollowUpToCommand(interaction, commandProcessors);

        return Ok();
    }

    [HttpPost("sisyphish/event")]
    [GoogleCloud]
    public async Task<IActionResult> ProcessEvent(DiscordInteraction interaction)
    {
        // var commandProcessors = _commandProcessors
        //     .OfType<MessageComponentCommandProcessor>()
        //     .ToList<ICommandProcessor>();
        
        // await ProcessFollowUpToCommand(interaction, commandProcessors);
        
        // return Ok();

        var initFisherResult = await InitFisher(interaction);
        var fisher = initFisherResult?.Fisher;
        
        var initPromptResult = await InitPrompt(interaction);
        var prompt = initPromptResult?.Prompt;

        if (fisher != null && prompt != null && initFisherResult?.InitSuccess == true && initPromptResult?.InitSuccess == true)
        {
            switch (prompt?.Event)
            {
                case Event.FoundTreasureChest:
                    var item = interaction.PromptResponse == "open"
                        ? (Item?)Random.Shared.GetItems(Enum.GetValues<Item>(), 1).Single()
                        : null;
                    
                    if (item != null)
                    {
                        await AddItem(fisher, item.Value);
                    }
                    await DeletePrompt(interaction);
                    await UpdateDiscordForEvent(interaction, initFisherResult, initPromptResult, item);
                    
                    break;
                default:
                    break;
            }
        }
        else
        {
            await _discord.DeleteResponse(interaction);
        }

        await UnlockFisher(fisher);

        return Ok();
    }

    [HttpPost("sisyphish/reset")]
    [GoogleCloud]
    public async Task<IActionResult> ProcessResetCommand(DiscordInteraction interaction)
    {
        var commandProcessors = _commandProcessors
            .Where(p => p.Command == DiscordCommandName.Reset)
            .ToList();
        
        await ProcessFollowUpToCommand(interaction, commandProcessors);
        
        return Ok();
    }

    private async Task ProcessFollowUpToCommand(DiscordInteraction interaction, List<ICommandProcessor> processors)
    {
        if (processors.Count != 1)
        {
            await ServeError(interaction, "An unexpected error occurred, please try again later!");
        }

        var commandProcessor = processors.Single();

        await commandProcessor.ProcessFollowUpToCommand(interaction);
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

    private async Task AddItem(Fisher fisher, Item item)
    {
        await _firestoreDb.Collection("fishers")
            .Document(fisher.Id)
            .UpdateAsync("items", FieldValue.ArrayUnion(item.ToString()));
    }

    private async Task DeletePrompt(DiscordInteraction interaction)
    {

        var documents = await _firestoreDb.Collection("prompts")
            .WhereEqualTo("discord_user_id", interaction.UserId)
            .WhereEqualTo("discord_prompt_id", interaction.PromptId)
            .Limit(1)
            .GetSnapshotAsync();
        
        var document = documents.SingleOrDefault();

        if (document != null)
        {
            await document.Reference.DeleteAsync();
        }
    }

    private async Task UpdateDiscordForEvent(DiscordInteraction interaction, InitFisherResult? initFisherResult, InitPromptResult? initPromptResult, Item? item)
    {
        try
        {
            if (initFisherResult?.Fisher == null)
            {
                await ServeError(interaction, "An unexpected error occurred, please try again later!");
            }
            else if (initPromptResult?.Prompt == null)
            {
                await ServeError(interaction, "An unexpected error occurred, please try again later!");
            }
            else if (!initFisherResult.InitSuccess)
            {
                await ServeError(interaction, $"<@{initFisherResult.Fisher.DiscordUserId}>, you are sending messages too quickly, please try again in a moment!");
            }
            else if (!initPromptResult.InitSuccess)
            {
                await ServeError(interaction, $"<@{initFisherResult.Fisher.DiscordUserId}>, you are sending messages too quickly, please try again in a moment!");
            }
            else if (item == null)
            {
                var content = $"You get nothing!";

                await _discord.EditResponse(interaction, content, []);
            }
            else
            {
                var content = $"Inside the chest was: 1 {item}!";

                await _discord.EditResponse(interaction, content, []);
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
}
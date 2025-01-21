using Google.Cloud.Firestore;
using Grpc.Core;
using sisyphish.Discord.Models;
using sisyphish.Sisyphish.Models;

namespace sisyphish.Sisyphish.Services;

public class FirestoreDbFisherService : IFisherService
{
    private readonly FirestoreDb _firestoreDb;
    private readonly ILogger<FirestoreDbFisherService> _logger;

    public FirestoreDbFisherService(FirestoreDb firestoreDb, ILogger<FirestoreDbFisherService> logger)
    {
        _firestoreDb = firestoreDb;
        _logger = logger;
    }

    public async Task DeleteFisher(DiscordInteraction interaction)
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

    public async Task<InitFisherResult?> InitFisher(DiscordInteraction interaction)
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
                catch (RpcException ex) when (ex.StatusCode == StatusCode.FailedPrecondition)
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
    
    public async Task<Fisher?> GetFisher(DiscordInteraction interaction)
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

    public async Task<Fisher?> CreateFisher(DiscordInteraction interaction)
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

    public async Task LockFisher(Fisher fisher)
    {
        await _firestoreDb.Collection("fishers")
            .Document(fisher.Id)
            .UpdateAsync("locked_at", DateTime.UtcNow, Precondition.LastUpdated(Timestamp.FromDateTime(fisher.LastUpdated!.Value)));
    }

    public async Task UnlockFisher(Fisher? fisher)
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

    public async Task AddFish(Fisher fisher, FishType fishType, long fishSize)
    {
        var fishes = fisher.Fish;
        var fish = fishes.SingleOrDefault(f => f.Type == fishType);

        if (fish != null)
        {
            fish.Count++;
        }
        else
        {
            fishes.Add(new Fish { Type = fishType, Count = 1 });
        }

        await _firestoreDb.Collection("fishers")
            .Document(fisher.Id)
            .UpdateAsync(new Dictionary<string, object>
            {
                { "fish_caught", FieldValue.Increment(1) },
                { "biggest_fish", Math.Max(fisher.BiggestFish ?? 0, fishSize) },
                { "fish", fishes }
            });
    }

    public async Task AddItem(Fisher fisher, ItemType itemType)
    {
        var items = fisher.Items;
        var item = items.SingleOrDefault(i => i.Type == itemType);

        if (item != null)
        {
            item.Count++;
        }
        else
        {
            items.Add(new Item { Type = itemType, Count = 1 });
        }

        await _firestoreDb.Collection("fishers")
            .Document(fisher.Id)
            .UpdateAsync("items", items);
    }

    public async Task CreatePrompt(DiscordInteraction interaction, Expedition expedition)
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

    public async Task<InitPromptResult?> InitPrompt(DiscordInteraction interaction)
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

    public async Task<Prompt?> GetPrompt(DiscordInteraction interaction)
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

    public async Task LockPrompt(Prompt prompt)
    {
        await _firestoreDb.Collection("prompts")
            .Document(prompt.Id)
            .UpdateAsync("locked_at", DateTime.UtcNow, Precondition.LastUpdated(Timestamp.FromDateTime(prompt.LastUpdated!.Value)));
    }

    public async Task DeletePrompt(DiscordInteraction interaction)
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
}
using sisyphish.Discord.Models;
using sisyphish.Extensions;
using sisyphish.GoogleCloud.Firestore;
using sisyphish.Sisyphish.Models;

namespace sisyphish.Sisyphish.Services;

public class FirestoreDbFisherService : IFisherService
{
    private const string DocumentType = "fishers";

    private readonly IFirestoreService _firestore;
    private readonly ILogger<FirestoreDbFisherService> _logger;

    public FirestoreDbFisherService(IFirestoreService firestore, ILogger<FirestoreDbFisherService> logger)
    {
        _firestore = firestore;
        _logger = logger;
    }

    public async Task DeleteFisher(DiscordInteraction interaction)
    {
        var fisher = await GetFisher(interaction);

        if (fisher != null)
        {
            var deleteRequest = new DeleteFirestoreDocumentRequest
            {
                DocumentId = fisher.Id,
                DocumentType = DocumentType
            };

            await _firestore.DeleteDocument(deleteRequest);
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
                await LockFisher(fisher);
                result.InitSuccess = true;
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
        var document = await _firestore.GetDocumentByField(DocumentType, "discord_user_id", interaction.UserId);
        if (document == null)
        {
            return null;
        }

        var fisher = DeserializeFisher(document);
        return fisher;
    }

    private static Fisher? DeserializeFisher(GoogleCloudFirestoreDocument? document)
    {
        if (document == null)
        {
            return null;
        }

        var fisher = new Fisher
        {
            Id = document.Id,
            LastUpdated = document.UpdateTime,
            CreatedAt = GoogleCloudFirestoreDocument.ParseDateTime(document.CreateTime),
            DiscordUserId = document.GetString("discord_user_id"),
            FishCaught = document.GetLong("fish_caught"),
            BiggestFish = document.GetLong("biggest_fish"),
            LockedAt = document.GetTimestamp("locked_at"),
            Fish = document.GetList("fish")
                .Select(fishDoc => new Fish
                {
                    Type = fishDoc.GetEnum<FishType>("type"),
                    Count = fishDoc.GetLong("count")
                })
                .ToList(),
            Items = document.GetList("items")
                .Select(itemDoc => new Item
                {
                    Type = itemDoc.GetEnum<ItemType>("type"),
                    Count = itemDoc.GetLong("count")
                })
                .ToList()
        };

        return fisher;
    }

    private static Dictionary<string, GoogleCloudFirestoreValue>? SerializeFisherFields(Fisher? fisher)
    {
        if (fisher == null)
        {
            return null;
        }

        var fields = new Dictionary<string, GoogleCloudFirestoreValue>() //TODO: created_at
            .AddIfNotNull("discord_user_id", fisher.DiscordUserId)
            .AddIfNotNull("fish_caught", fisher.FishCaught)
            .AddIfNotNull("biggest_fish", fisher.BiggestFish)
            .AddIfNotNull("locked_at", fisher.LockedAt)
            .AddIfNotNull("fish", fisher.Fish, fish => new GoogleCloudFirestoreValue
            {
                MapValue = new GoogleCloudFirestoreMapValue
                {
                    Fields = new Dictionary<string, GoogleCloudFirestoreValue>()
                        .AddIfNotNull("type", fish.Type.ToString())
                        .AddIfNotNull("count", fish.Count)
                }
            })
            .AddIfNotNull("items", fisher.Items, item => new GoogleCloudFirestoreValue
            {
                MapValue = new GoogleCloudFirestoreMapValue
                {
                    Fields = new Dictionary<string, GoogleCloudFirestoreValue>()
                        .AddIfNotNull("type", item.Type.ToString())
                        .AddIfNotNull("count", item.Count)
                }
            });

        return fields;
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

        var createRequest = new CreateFirestoreDocumentRequest
        {
            DocumentType = DocumentType,
            Fields = SerializeFisherFields(fisher)!
        };

        var createResponse = await _firestore.CreateDocument(createRequest);

        fisher.Id = createResponse?.Id;
        fisher.LastUpdated = createResponse?.UpdateTime;

        return fisher;
    }

    public async Task LockFisher(Fisher fisher)
    {
        fisher.LockedAt = DateTime.UtcNow;

        var updateRequest = new UpdateFirestoreDocumentRequest
        {
            DocumentId = fisher.Id!,
            DocumentType = DocumentType,
            Fields = SerializeFisherFields(fisher)!,
            CurrentDocument = new UpdateFirestoreDocumentPrecondition
            {
                UpdateTime = fisher.LastUpdated
            }
        };

        var updateResponse = await _firestore.UpdateDocument(updateRequest);

        fisher.LastUpdated = updateResponse?.UpdateTime;
    }

    public async Task UnlockFisher(Fisher? fisher)
    {
        if (fisher == null)
        {
            return;
        }

        fisher.LockedAt = null;

        var updateRequest = new UpdateFirestoreDocumentRequest
        {
            DocumentId = fisher.Id!,
            DocumentType = DocumentType,
            Fields = SerializeFisherFields(fisher)!,
            CurrentDocument = new UpdateFirestoreDocumentPrecondition
            {
                UpdateTime = fisher.LastUpdated
            }
        };

        var updateResponse = await _firestore.UpdateDocument(updateRequest);

        fisher.LastUpdated = updateResponse?.UpdateTime;
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

        fisher.FishCaught++;
        fisher.BiggestFish = Math.Max(fisher.BiggestFish ?? 0, fishSize);
        fisher.Fish = fishes;

        var updateRequest = new UpdateFirestoreDocumentRequest
        {
            DocumentId = fisher.Id!,
            DocumentType = DocumentType,
            Fields = SerializeFisherFields(fisher)!,
            CurrentDocument = new UpdateFirestoreDocumentPrecondition
            {
                UpdateTime = fisher.LastUpdated
            }
        };

        var updateResponse = await _firestore.UpdateDocument(updateRequest);

        fisher.LastUpdated = updateResponse?.UpdateTime;
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

        fisher.Items = items;

        var updateRequest = new UpdateFirestoreDocumentRequest
        {
            DocumentId = fisher.Id!,
            DocumentType = DocumentType,
            Fields = SerializeFisherFields(fisher)!,
            CurrentDocument = new UpdateFirestoreDocumentPrecondition
            {
                UpdateTime = fisher.LastUpdated
            }
        };

        var updateResponse = await _firestore.UpdateDocument(updateRequest);

        fisher.LastUpdated = updateResponse?.UpdateTime;
    }
}
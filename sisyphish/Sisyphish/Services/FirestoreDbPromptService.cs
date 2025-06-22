using sisyphish.Discord.Models;
using sisyphish.Extensions;
using sisyphish.GoogleCloud.Firestore;
using sisyphish.Sisyphish.Models;

namespace sisyphish.Sisyphish.Services;

public class FirestoreDbPromptService : IPromptService
{
    private const string DocumentType = "prompts";
    
    private readonly IFirestoreService _firestore;
    private readonly ILogger<FirestoreDbPromptService> _logger;

    public FirestoreDbPromptService(IFirestoreService firestore, ILogger<FirestoreDbPromptService> logger)
    {
        _firestore = firestore;
        _logger = logger;
    }

    private static Prompt? DeserializePrompt(GoogleCloudFirestoreDocument? document)
    {
        if (document == null)
        {
            return null;
        }

        var prompt = new Prompt
        {
            Id = document.Id,
            LastUpdated = GoogleCloudFirestoreDocument.ParseDateTime(document.UpdateTime),
            CreatedAt = GoogleCloudFirestoreDocument.ParseDateTime(document.CreateTime),
            DiscordUserId = document.GetString("discord_user_id"),
            DiscordPromptId = document.GetString("discord_prompt_id"),
            LockedAt = document.GetTimestamp("locked_at"),
            Event = document.GetEnum<Event>("event")
        };

        return prompt;
    }

    private static Dictionary<string, GoogleCloudFirestoreValue>? SerializePromptFields(Prompt? prompt)
    {
        if (prompt == null)
        {
            return null;
        }

        var fields = new Dictionary<string, GoogleCloudFirestoreValue>()
            .AddIfNotNull("discord_user_id", prompt.DiscordUserId)
            .AddIfNotNull("discord_prompt_id", prompt.DiscordPromptId)
            .AddIfNotNull("locked_at", prompt.LockedAt)
            .AddIfNotNull("event", prompt.Event.ToString());

        return fields;
    }

    public async Task<Prompt?> CreatePrompt(DiscordInteraction interaction, Expedition expedition)
    {
        var prompt = new Prompt
        {
            CreatedAt = DateTime.UtcNow,
            DiscordUserId = interaction.UserId,
            DiscordPromptId = expedition.PromptId,
            Event = expedition.Event
        };

        var createRequest = new CreateFirestoreDocumentRequest
        {
            DocumentType = DocumentType,
            Fields = SerializePromptFields(prompt)!
        };

        var createResponse = await _firestore.CreateDocument(createRequest);

        prompt.Id = createResponse?.Id;
        prompt.LastUpdated = GoogleCloudFirestoreDocument.ParseDateTime(createResponse?.UpdateTime);

        return prompt;
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
                await LockPrompt(prompt);
                result.InitSuccess = true;
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
        var document = await _firestore.GetDocumentByFields(DocumentType, new Dictionary<string, string?>
        {
            { "discord_user_id", interaction.UserId },
            { "discord_prompt_id", interaction.PromptId }
        });

        if (document == null)
        {
            return null;
        }

        var prompt = DeserializePrompt(document);
        return prompt;
    }

    public async Task LockPrompt(Prompt prompt)
    {
        prompt.LockedAt = DateTime.UtcNow;

        var updateRequest = new UpdateFirestoreDocumentRequest
        {
            DocumentId = prompt.Id!,
            DocumentType = DocumentType,
            Fields = SerializePromptFields(prompt)!,
            CurrentDocument = new UpdateFirestoreDocumentPrecondition
            {
                UpdateTime = prompt.LastUpdated?.ToUniversalTime().ToString("O")
            }
        };

        await _firestore.UpdateDocument(updateRequest);
    }

    public async Task DeletePrompt(DiscordInteraction interaction)
    {
        var prompt = await GetPrompt(interaction);

        if (prompt != null)
        {
            var deleteRequest = new DeleteFirestoreDocumentRequest
            {
                DocumentId = prompt.Id,
                DocumentType = DocumentType
            };

            await _firestore.DeleteDocument(deleteRequest);
        }
    }
}
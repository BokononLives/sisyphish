using Google.Cloud.Firestore;
using sisyphish.GoogleCloud;

namespace sisyphish.Sisyphish.Models;

[FirestoreData]
public class Prompt : FirestoreDocument
{
    [FirestoreProperty("created_at")] public DateTime? CreatedAt { get; set; }
    [FirestoreProperty("discord_user_id")] public string? DiscordUserId { get; set; }
    [FirestoreProperty("discord_prompt_id")] public string? DiscordPromptId { get; set; }
    [FirestoreProperty("locked_at")] public DateTime? LockedAt { get; set; }
    [FirestoreProperty(ConverterType = typeof(FirestoreEnumNameConverter<Event>), Name = "event")] public Event Event { get; set; }
    public bool IsLocked => LockedAt != null && LockedAt < DateTime.UtcNow.AddMinutes(-1);
}
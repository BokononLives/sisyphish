using sisyphish.GoogleCloud.Firestore;

namespace sisyphish.Sisyphish.Models;

public class Prompt : FirestoreDocument
{
    public DateTime? CreatedAt { get; set; }
    public string? DiscordUserId { get; set; }
    public string? DiscordPromptId { get; set; }
    public DateTime? LockedAt { get; set; }
    public Event? Event { get; set; }
    public bool IsLocked => LockedAt != null && LockedAt > DateTime.UtcNow.AddMinutes(-1);
}
using sisyphish.GoogleCloud.Firestore;

namespace sisyphish.Sisyphish.Models;

public class Fisher : FirestoreDocument
{
    public DateTime? CreatedAt { get; set; }
    public string? DiscordUserId { get; set; }
    public long? FishCaught { get; set; }
    public long? BiggestFish { get; set; }
    public DateTime? LockedAt { get; set; }
    public List<Item> Items { get; set; } = [];
    public List<Fish> Fish { get; set; } = [];
    public bool IsLocked => LockedAt != null && LockedAt > DateTime.UtcNow.AddMinutes(-1);
}
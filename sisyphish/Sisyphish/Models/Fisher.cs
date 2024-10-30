using Google.Cloud.Firestore;

namespace sisyphish.Sisyphish.Models;

[FirestoreData]
public class Fisher
{
    [FirestoreProperty("id")] public string? Id { get; set; }
    [FirestoreProperty("created_at")] public DateTime? CreatedAt { get; set; }
    [FirestoreProperty("discord_user_id")] public string? DiscordUserId { get; set; }
    [FirestoreProperty("fish_caught")] public List<Fish> Fish { get; set; } = [];
    [FirestoreProperty("locked_at")] public DateTime? LockedAt { get; set; }
    public long? FishCaught => Fish.Count();
    public long? BiggestFish => Fish.Max(f => f.Size);
    public DateTime? LastUpdated { get; set; }
}
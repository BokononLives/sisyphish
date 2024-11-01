using Google.Cloud.Firestore;
using sisyphish.GoogleCloud;

namespace sisyphish.Sisyphish.Models;

[FirestoreData]
public class Fisher : FirestoreDocument
{
    [FirestoreProperty("created_at")] public DateTime? CreatedAt { get; set; }
    [FirestoreProperty("discord_user_id")] public string? DiscordUserId { get; set; }
    [FirestoreProperty("fish_caught")] public long? FishCaught { get; set; }
    [FirestoreProperty("biggest_fish")] public long? BiggestFish { get; set; }
    [FirestoreProperty("locked_at")] public DateTime? LockedAt { get; set; }
    public bool IsLocked => LockedAt != null && LockedAt < DateTime.UtcNow.AddMinutes(-1);
}
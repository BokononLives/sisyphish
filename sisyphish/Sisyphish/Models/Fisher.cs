using Google.Cloud.Firestore;

namespace sisyphish.Sisyphish.Models;

public class Fisher
{
    [FirestoreProperty("id")] public string? Id { get; set; }
    [FirestoreProperty("created_at")] public DateTime? CreatedAt { get; set; }
    [FirestoreProperty("discord_user_id")] public string? DiscordUserId { get; set; }
    [FirestoreProperty("fish_caught")] public List<Dictionary<string, object>> Fish { get; set; } = new();
    public int? FishCaught => Fish.Count();
    public long? BiggestFish => Fish.Max(f => (long)f["size"]);
}
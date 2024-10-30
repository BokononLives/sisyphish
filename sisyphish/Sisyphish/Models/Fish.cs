using Google.Cloud.Firestore;

namespace sisyphish.Sisyphish.Models;

[FirestoreData]
public class Fish
{
    [FirestoreProperty("type")] public string? Type { get; set; }
    [FirestoreProperty("size")] public long? Size { get; set; }
}
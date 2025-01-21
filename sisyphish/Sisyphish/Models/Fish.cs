using Google.Cloud.Firestore;

namespace sisyphish.Sisyphish.Models;

[FirestoreData]
public class Fish
{
    [FirestoreProperty(ConverterType = typeof(FirestoreEnumNameConverter<FishType>), Name = "type")] public FishType Type { get; set; }
    [FirestoreProperty("count")] public long? Count { get; set; }
}
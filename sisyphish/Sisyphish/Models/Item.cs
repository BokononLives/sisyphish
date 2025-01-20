using Google.Cloud.Firestore;

namespace sisyphish.Sisyphish.Models;

[FirestoreData]
public class Item
{
    [FirestoreProperty(ConverterType = typeof(FirestoreEnumNameConverter<ItemType>), Name = "type")] public ItemType Type { get; set; }
    [FirestoreProperty("count")] public long? Count { get; set; }
}
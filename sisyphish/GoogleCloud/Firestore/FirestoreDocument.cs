namespace sisyphish.GoogleCloud.Firestore;

public abstract class FirestoreDocument
{
    public string? Id { get; set; }
    public string? LastUpdated { get; set; }
}
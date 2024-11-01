namespace sisyphish.GoogleCloud;

public abstract class FirestoreDocument
{
    public string? Id { get; set; }
    public DateTime? LastUpdated { get; set; }
}
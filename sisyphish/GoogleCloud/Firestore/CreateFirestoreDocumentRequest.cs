using System.Text.Json.Serialization;

namespace sisyphish.GoogleCloud.Firestore;

public class CreateFirestoreDocumentRequest
{
    [JsonIgnore] public string DocumentId { get; set; } = Guid.NewGuid().ToString();
    [JsonIgnore] public string? DocumentType { get; set; }
    public Dictionary<string, GoogleCloudFirestoreValue> Fields { get; set; } = default!;
}
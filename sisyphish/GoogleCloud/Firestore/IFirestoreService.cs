namespace sisyphish.GoogleCloud.Firestore;

public interface IFirestoreService
{
    Task<GoogleCloudFirestoreDocument?> GetDocumentById(string documentType, string documentId);
    Task<GoogleCloudFirestoreDocument?> GetDocumentByField(string documentType, string fieldName, string? fieldValue);
    Task<GoogleCloudFirestoreDocument?> GetDocumentByFields(string documentType, Dictionary<string, string?> fields);
    Task<GoogleCloudFirestoreDocument?> CreateDocument(CreateFirestoreDocumentRequest request);
    Task<GoogleCloudFirestoreDocument?> UpdateDocument(UpdateFirestoreDocumentRequest request);
    Task DeleteDocument(DeleteFirestoreDocumentRequest request);
}
using System.Text;
using sisyphish.GoogleCloud.Authentication;
using sisyphish.Tools;

namespace sisyphish.GoogleCloud.Firestore;

public class FirestoreService : GoogleCloudService, IFirestoreService
{
    public FirestoreService(ILogger<FirestoreService> logger, IGoogleCloudAuthenticationService authenticationService, HttpClient httpClient) : base(logger, authenticationService, httpClient)
    {
    }

    public async Task<GoogleCloudFirestoreDocument?> GetDocumentById(string documentType, string documentId)
    {
        await Authenticate();

        var firestoreDocument = await _httpClient.GetFromJsonAsync(
            requestUri: "databases/(default)/documents/{documentType}/{documentId}",
            jsonTypeInfo: CamelCaseJsonContext.Default.GoogleCloudFirestoreDocument
        );

        return firestoreDocument;
    }

    public async Task<GoogleCloudFirestoreDocument?> GetDocumentByFields(string documentType, Dictionary<string, string?> fields)
    {
        await Authenticate();

        var queryRequest = new GoogleCloudFirestoreQueryRequest
        {
            StructuredQuery = new GoogleCloudFirestoreStructuredQuery
            {
                From = { new GoogleCloudFirestoreCollectionSelector { CollectionId = documentType } },
                Where = new GoogleCloudFirestoreWhereClause
                {
                    CompositeFilter = new GoogleCloudFirestoreCompositeFilter
                    {
                        Op = "AND",
                        Filters = fields.Select(f => new GoogleCloudFirestoreWhereClause
                        {
                            FieldFilter = new GoogleCloudFirestoreFieldFilter
                            {
                                Field = new GoogleCloudFirestoreFieldReference { FieldPath = f.Key },
                                Op = "EQUAL",
                                Value = new GoogleCloudFirestoreValue { StringValue = f.Value }
                            }
                        }).ToList()
                    }
                }
            }
        };

        var httpResponse = await _httpClient.PostAsJsonAsync(
            requestUri: "databases/(default)/documents:runQuery",
            value: queryRequest,
            jsonTypeInfo: CamelCaseJsonContext.Default.GoogleCloudFirestoreQueryRequest
        );

        try
        {
            httpResponse.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, await httpResponse.Content.ReadAsStringAsync());
        }

        var documents = await httpResponse.Content.ReadFromJsonAsync(
            jsonTypeInfo: CamelCaseJsonContext.Default.ListGoogleCloudFirestoreQueryResponse
        );

        return documents?.FirstOrDefault()?.Document;
    }

    public async Task<GoogleCloudFirestoreDocument?> GetDocumentByField(string documentType, string fieldName, string? fieldValue)
    {
        await Authenticate();

        var queryRequest = new GoogleCloudFirestoreQueryRequest
        {
            StructuredQuery = new GoogleCloudFirestoreStructuredQuery
            {
                From = { new GoogleCloudFirestoreCollectionSelector { CollectionId = documentType } },
                Where = new GoogleCloudFirestoreWhereClause
                {
                    FieldFilter = new GoogleCloudFirestoreFieldFilter
                    {
                        Field = new GoogleCloudFirestoreFieldReference { FieldPath = fieldName },
                        Op = "EQUAL",
                        Value = new GoogleCloudFirestoreValue { StringValue = fieldValue }
                    }
                }
            }
        };

        var httpResponse = await _httpClient.PostAsJsonAsync(
            requestUri: "databases/(default)/documents:runQuery",
            value: queryRequest,
            jsonTypeInfo: CamelCaseJsonContext.Default.GoogleCloudFirestoreQueryRequest
        );

        httpResponse.EnsureSuccessStatusCode();

        var documents = await httpResponse.Content.ReadFromJsonAsync(
            jsonTypeInfo: CamelCaseJsonContext.Default.ListGoogleCloudFirestoreQueryResponse
        );

        return documents?.FirstOrDefault()?.Document;
    }

    public async Task<GoogleCloudFirestoreDocument?> CreateDocument(CreateFirestoreDocumentRequest request)
    {
        await Authenticate();

        var httpResponse = await _httpClient.PostAsJsonAsync(
            requestUri: "databases/(default)/documents/{request.DocumentType}?documentId={request.DocumentId}",
            value: request,
            jsonTypeInfo: CamelCaseJsonContext.Default.CreateFirestoreDocumentRequest
        );

        httpResponse.EnsureSuccessStatusCode();

        var document = await httpResponse.Content.ReadFromJsonAsync(CamelCaseJsonContext.Default.GoogleCloudFirestoreDocument);

        return document;
    }

    public async Task<GoogleCloudFirestoreDocument?> UpdateDocument(UpdateFirestoreDocumentRequest request)
    {
        await Authenticate();

        var queryString = new StringBuilder();

        if (request.CurrentDocument?.UpdateTime != null)
        {
            queryString.Append($"currentDocument.updateTime={request.CurrentDocument.UpdateTime}");
        }

        var httpResponse = await _httpClient.PatchAsJsonAsync(
            requestUri: "databases/(default)/documents/{request.DocumentType}/{request.DocumentId}?{queryString}",
            value: request,
            jsonTypeInfo: CamelCaseJsonContext.Default.UpdateFirestoreDocumentRequest
        );

        httpResponse.EnsureSuccessStatusCode();

        var document = await httpResponse.Content.ReadFromJsonAsync(CamelCaseJsonContext.Default.GoogleCloudFirestoreDocument);

        return document;
    }

    public async Task DeleteDocument(DeleteFirestoreDocumentRequest request)
    {
        await Authenticate();

        var httpResponse = await _httpClient.DeleteAsync(
            requestUri: "databases/(default)/documents/{request.DocumentType}/{request.DocumentId}"
        );

        httpResponse.EnsureSuccessStatusCode();
    }
}
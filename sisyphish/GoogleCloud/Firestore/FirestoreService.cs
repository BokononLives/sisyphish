using System.Text;
using sisyphish.Tools;

namespace sisyphish.GoogleCloud.Firestore;

public class FirestoreService : IFirestoreService
{
    private readonly ILogger<FirestoreService> _logger;

    private string? _accessToken;
    private DateTime? _accessTokenExpirationDate;

    public FirestoreService(ILogger<FirestoreService> logger)
    {
        _logger = logger;
    }

    private async Task<string> GetAccessToken()
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && (_accessTokenExpirationDate == null || _accessTokenExpirationDate > DateTime.UtcNow))
        {
            return _accessToken;
        }

        using var httpClient = new HttpClient { DefaultRequestHeaders = { { "Metadata-Flavor", "Google" } } };

        var accessTokenResponse = await httpClient.GetFromJsonAsync(
            requestUri: $"{Config.GoogleMetadataBaseUrl}/computeMetadata/v1/instance/service-accounts/default/token",
            jsonTypeInfo: SnakeCaseJsonContext.Default.GoogleCloudAccessToken
        );

        if (string.IsNullOrWhiteSpace(accessTokenResponse?.AccessToken))
        {
            _logger.LogError(@$"Google Access Token was unexpectedly null:
                - response: {accessTokenResponse}");

            throw new Exception("Unable to acquire Google Access token");
        }

        _accessToken = accessTokenResponse.AccessToken;
        _accessTokenExpirationDate = DateTime.UtcNow.AddSeconds((accessTokenResponse.ExpiresIn ?? 0) - 60);

        return _accessToken;
    }

    private async Task<HttpClient> GetBaseHttpClient()
    {
        var accessToken = await GetAccessToken();

        var httpClient = new HttpClient { DefaultRequestHeaders = { { "Authorization", $"Bearer {accessToken}" } } };
        return httpClient;
    }

    public async Task<GoogleCloudFirestoreDocument?> GetDocumentById(string documentType, string documentId)
    {
        using var httpClient = await GetBaseHttpClient();

        var firestoreDocument = await httpClient.GetFromJsonAsync(
            requestUri: $"{Config.GoogleFirestoreBaseUrl}/databases/(default)/documents/{documentType}/{documentId}",
            jsonTypeInfo: CamelCaseJsonContext.Default.GoogleCloudFirestoreDocument
        );

        return firestoreDocument;
    }

    public async Task<GoogleCloudFirestoreDocument?> GetDocumentByFields(string documentType, Dictionary<string, string?> fields)
    {
        using var httpClient = await GetBaseHttpClient();

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
                        Filters = fields.Select(f => new GoogleCloudFirestoreFieldFilter
                        {
                            Field = new GoogleCloudFirestoreFieldReference { FieldPath = f.Key },
                            Op = "EQUAL",
                            Value = new GoogleCloudFirestoreValue { StringValue = f.Value }
                        }).ToList()
                    }
                }
            }
        };

        var httpResponse = await httpClient.PostAsJsonAsync(
            requestUri: $"{Config.GoogleFirestoreBaseUrl}/databases/(default)/documents:runQuery",
            value: queryRequest,
            jsonTypeInfo: CamelCaseJsonContext.Default.GoogleCloudFirestoreQueryRequest
        );

        try
        {
            httpResponse.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, await httpResponse.Content.ReadAsStringAsync());
        }

        var documents = await httpResponse.Content.ReadFromJsonAsync(
            jsonTypeInfo: CamelCaseJsonContext.Default.ListGoogleCloudFirestoreQueryResponse
        );

        return documents?.FirstOrDefault()?.Document;
    }

    public async Task<GoogleCloudFirestoreDocument?> GetDocumentByField(string documentType, string fieldName, string? fieldValue)
    {
        using var httpClient = await GetBaseHttpClient();

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

        var httpResponse = await httpClient.PostAsJsonAsync(
            requestUri: $"{Config.GoogleFirestoreBaseUrl}/databases/(default)/documents:runQuery",
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
        using var httpClient = await GetBaseHttpClient();

        var httpResponse = await httpClient.PostAsJsonAsync(
            requestUri: $"{Config.GoogleFirestoreBaseUrl}/databases/(default)/documents/{request.DocumentType}?documentId={request.DocumentId}",
            value: request,
            jsonTypeInfo: CamelCaseJsonContext.Default.CreateFirestoreDocumentRequest
        );

        httpResponse.EnsureSuccessStatusCode();

        var document = await httpResponse.Content.ReadFromJsonAsync(CamelCaseJsonContext.Default.GoogleCloudFirestoreDocument);

        return document;
    }

    public async Task<GoogleCloudFirestoreDocument?> UpdateDocument(UpdateFirestoreDocumentRequest request)
    {
        using var httpClient = await GetBaseHttpClient();

        var queryString = new StringBuilder();

        if (request.CurrentDocument?.UpdateTime != null)
        {
            queryString.Append($"currentDocument.updateTime={request.CurrentDocument.UpdateTime}");
        }

        var httpResponse = await httpClient.PatchAsJsonAsync(
            requestUri: $"{Config.GoogleFirestoreBaseUrl}/databases/(default)/documents/{request.DocumentType}/{request.DocumentId}?{queryString}",
            value: request,
            jsonTypeInfo: CamelCaseJsonContext.Default.UpdateFirestoreDocumentRequest
        );

        try
        {
            httpResponse.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            var foo = await httpResponse.Content.ReadAsStringAsync();
            _logger.LogError(ex, foo ?? "unknown error");
        }

        var document = await httpResponse.Content.ReadFromJsonAsync(CamelCaseJsonContext.Default.GoogleCloudFirestoreDocument);

        return document;
    }

    public async Task DeleteDocument(DeleteFirestoreDocumentRequest request)
    {
        using var httpClient = await GetBaseHttpClient();

        var httpResponse = await httpClient.DeleteAsync(
            requestUri: $"{Config.GoogleFirestoreBaseUrl}/databases/(default)/documents/{request.DocumentType}/{request.DocumentId}"
        );

        httpResponse.EnsureSuccessStatusCode();
    }
}
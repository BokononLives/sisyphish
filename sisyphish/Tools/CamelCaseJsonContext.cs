using System.Text.Json.Serialization;
using sisyphish.GoogleCloud.CloudTasks;
using sisyphish.GoogleCloud.Firestore;
using sisyphish.GoogleCloud.Logging;

namespace sisyphish.Tools;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
[JsonSerializable(typeof(GoogleCloudTaskRequest))]
[JsonSerializable(typeof(GoogleCloudTask))]
[JsonSerializable(typeof(GoogleCloudHttpRequest))]
[JsonSerializable(typeof(GoogleCloudOidcToken))]
[JsonSerializable(typeof(GoogleCloudFirestoreDocument))]
[JsonSerializable(typeof(GoogleCloudFirestoreValue))]
[JsonSerializable(typeof(GoogleCloudFirestoreQueryRequest))]
[JsonSerializable(typeof(GoogleCloudFirestoreStructuredQuery))]
[JsonSerializable(typeof(GoogleCloudFirestoreCollectionSelector))]
[JsonSerializable(typeof(GoogleCloudFirestoreWhereClause))]
[JsonSerializable(typeof(GoogleCloudFirestoreFieldFilter))]
[JsonSerializable(typeof(GoogleCloudFirestoreFieldReference))]
[JsonSerializable(typeof(GoogleCloudFirestoreQueryResponse))]
[JsonSerializable(typeof(List<GoogleCloudFirestoreQueryResponse>))]
[JsonSerializable(typeof(CreateFirestoreDocumentRequest))]
[JsonSerializable(typeof(UpdateFirestoreDocumentRequest))]
[JsonSerializable(typeof(UpdateFirestoreDocumentPrecondition))]
[JsonSerializable(typeof(GoogleCloudLoggingJsonPayload))]
[JsonSerializable(typeof(GoogleCloudLoggingLogEntry))]
[JsonSerializable(typeof(GoogleCloudLoggingLogRequest))]
[JsonSerializable(typeof(FallbackErrorLog))]
[JsonSerializable(typeof(string))]
internal partial class CamelCaseJsonContext : JsonSerializerContext
{
}

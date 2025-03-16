using System.Text.Json.Serialization;
using sisyphish.GoogleCloud.Models;

namespace sisyphish.Tools;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
[JsonSerializable(typeof(GoogleCloudTaskRequest))]
[JsonSerializable(typeof(GoogleCloudTask))]
[JsonSerializable(typeof(GoogleCloudHttpRequest))]
[JsonSerializable(typeof(GoogleCloudOidcToken))]
internal partial class CamelCaseJsonContext : JsonSerializerContext
{
}
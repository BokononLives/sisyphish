using System.Text.Json.Serialization;
using sisyphish.GoogleCloud.Models;

namespace sisyphish;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
[JsonSerializable(typeof(GoogleCloudTaskRequest))]
[JsonSerializable(typeof(GoogleCloudTask))]
[JsonSerializable(typeof(GoogleCloudHttpRequest))]
internal partial class CamelCaseJsonContext : JsonSerializerContext
{
}
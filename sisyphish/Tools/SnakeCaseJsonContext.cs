using System.Text.Json.Serialization;
using sisyphish.Discord.Models;
using sisyphish.GoogleCloud.Logging;
using sisyphish.GoogleCloud.Models;

namespace sisyphish.Tools;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower
)]
[JsonSerializable(typeof(DiscordInteraction))]
[JsonSerializable(typeof(DiscordDeferralCallbackResponse))]
[JsonSerializable(typeof(DiscordInteractionEdit))]
[JsonSerializable(typeof(GoogleCloudAccessToken))]
[JsonSerializable(typeof(GoogleCloudLoggingLogResource))]
internal partial class SnakeCaseJsonContext : JsonSerializerContext
{
}
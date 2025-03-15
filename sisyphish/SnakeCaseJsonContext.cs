using System.Text.Json.Serialization;
using sisyphish.Discord.Models;
using sisyphish.GoogleCloud.Models;

namespace sisyphish;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower
)]
[JsonSerializable(typeof(DiscordInteraction))]
[JsonSerializable(typeof(DiscordDeferralCallbackResponse))]
[JsonSerializable(typeof(DiscordInteractionEdit))]
[JsonSerializable(typeof(GoogleCloudAccessToken))]
internal partial class SnakeCaseJsonContext : JsonSerializerContext
{
}
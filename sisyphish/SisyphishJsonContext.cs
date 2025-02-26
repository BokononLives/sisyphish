using System.Text.Json.Serialization;
using sisyphish.Discord.Models;

namespace sisyphish;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase // AOT-Compatible Naming Policy
)]
[JsonSerializable(typeof(DiscordInteraction))]
[JsonSerializable(typeof(DiscordDeferralCallbackResponse))]
[JsonSerializable(typeof(DiscordInteractionEdit))]
internal partial class SisyphishJsonContext : JsonSerializerContext
{
}
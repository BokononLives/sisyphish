using System.Text.Json;
using System.Text.Json.Serialization;

namespace sisyphish.Tools;

public static class JsonHelpers
{
    public static void SetupDefaultJsonSerializerOptions(JsonSerializerOptions options)
    {
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
        options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    }
}

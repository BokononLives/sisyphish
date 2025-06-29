namespace sisyphish.Tools;

public class Config
{
    public static string BaseUrl => IsDevelopment ? "http://localhost" : "http://0.0.0.0";
    public static string DiscordApplicationId => GetValue("DISCORD_APPLICATION_ID");
    public static string DiscordBaseUrl => "https://discord.com/api/v10/";
    public static string DiscordPublicKey => GetValue("DISCORD_PUBLIC_KEY");
    public static string DiscordToken => GetValue("DISCORD_TOKEN");
    public static string GoogleCertsBaseUrl => "https://www.googleapis.com/oauth2/v3/certs/";
    public static string GoogleCloudRunRevisionName => GetValue("K_REVISION", defaultValue: "unknown");
    public static string GoogleCloudRunServiceName => GetValue("K_SERVICE", defaultValue: "unknown");
    public static string GoogleFirestoreBaseUrl => $"https://firestore.googleapis.com/v1/projects/{GoogleProjectId}/";
    public static string GoogleLocation => "us-central1";
    public static string GoogleLoggingBaseUrl => "https://logging.googleapis.com/v2/";
    public static string GoogleLoggingLogName => "sisyphish";
    public static string GoogleMetadataBaseUrl => "http://metadata/";
    public static string GoogleProjectId => GetValue("GOOGLE_PROJECT_ID");
    public static string GoogleServiceAccount => GetValue("GOOGLE_SERVICE_ACCOUNT");
    public static string GoogleTasksBaseUrl => $"https://cloudtasks.googleapis.com/v2/projects/{GoogleProjectId}/locations/{GoogleLocation}/queues/{GoogleProjectId}/";
    public static bool IsDevelopment => GetValue("ASPNETCORE_ENVIRONMENT").Equals("DEVELOPMENT", StringComparison.InvariantCultureIgnoreCase);
    public static string Port => "8080";
    public static string PublicBaseUrl => "https://helloworld-33197368037.us-central1.run.app";
    public static string Url => $"{BaseUrl}:{Port}";

    private static string GetValue(string key, string? defaultValue = null)
    {
        return Environment.GetEnvironmentVariable(key) ?? defaultValue ?? string.Empty;
    }
}
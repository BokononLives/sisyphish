public class Config
{
    public static string DiscordApplicationId => GetValue("DISCORD_APPLICATION_ID");
    public static string DiscordPublicKey => GetValue("DISCORD_PUBLIC_KEY");
    public static string DiscordToken => GetValue("DISCORD_TOKEN");
    public static bool IsDevelopment => GetValue("ASPNETCORE_ENVIRONMENT").Equals("DEVELOPMENT", StringComparison.InvariantCultureIgnoreCase);
    public static string BaseUrl => IsDevelopment ? "http://localhost" : "http://0.0.0.0";
    public static string Port => "8080";
    public static string Url => $"{BaseUrl}:{Port}";

    private static string GetValue(string key)
    {
        return Environment.GetEnvironmentVariable(key) ?? string.Empty;
    }
}
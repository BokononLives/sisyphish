namespace sisyphish.Tools.Discord;

public class DiscordInteractionResponseData
{
    public string? Content { get; set; }
    public DiscordInteractionResponseFlags[] Flags { get; set; } = [];
}
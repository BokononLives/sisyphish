namespace sisyphish.Discord.Models;

public class DiscordInteractionEdit
{
    public string? Content { get; set; }
    public List<DiscordComponent> Components { get; set; } = [];
}
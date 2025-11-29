namespace sisyphish.Discord.Models;

public class DiscordComponent
{
    public DiscordMessageComponentType Type { get; set; }
    public string? CustomId { get; set; }
    public string? Label { get; set; }
    public DiscordButtonStyleType? Style { get; set; }
    public List<DiscordComponent> Components { get; set; } = [];
}

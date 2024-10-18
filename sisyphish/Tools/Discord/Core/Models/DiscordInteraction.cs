namespace sisyphish.Tools.Discord.Core.Models;

public class DiscordInteraction
{
    public DiscordInteractionType Type { get; set; }
    public DiscordInteractionData? Data { get; set; }
    public DiscordInteractionContext? Context { get; set; }
    public DiscordInteractionMember? Member { get; set; }
    public DiscordInteractionUser? User { get; set; }

    public string? UserId => Context == DiscordInteractionContext.Guild ? Member?.User?.Id : User?.Id;
}
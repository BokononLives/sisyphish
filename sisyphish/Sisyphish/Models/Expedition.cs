using System.Text;
using sisyphish.Discord.Models;

namespace sisyphish.Sisyphish.Models;

public class Expedition
{
    private string? _userId;

    public Expedition(string? userId)
    {
        _userId = userId;
        PromptId = Guid.NewGuid().ToString();
    }

    public Event Event { get; set; }
    public int? FishSize { get; set; }
    public bool? CaughtFish { get; set; }
    public string? PromptId { get; }

    public string GetContent(Fisher fisher)
    {
        var result = new StringBuilder();
        result.AppendLine($"You cast your line into the Sea of Possibilities...");

        if (Event == Event.FoundTreasureChest)
        {
            result.AppendLine("Lucky! You hooked a treasure chest!");
            return result.ToString();
        }

        if (FishSize == null)
        {
            result.AppendLine("...But nothing's biting!");
            return result.ToString();
        }
        else if (FishSize <= 2)
        {
            result.AppendLine("You feel the smallest nibble...");
        }
        else if (FishSize <= 5)
        {
            result.AppendLine("Something's biting!");
        }
        else if (FishSize <= 20)
        {
            result.AppendLine("Something's biting, and it's a pretty big one!");
        }
        else if (FishSize <= 50)
        {
            result.AppendLine("Something's biting, and it's a HUGE one!");
        }
        else
        {
            result.AppendLine("It's a massive trophy fish! Don't let this one get away!");
        }

        if (CaughtFish == true)
        {
            result.AppendLine($"You reel it in! Congratulations! You got a fish. It's {FishSize} cm!");
                
            if (FishSize > fisher!.BiggestFish)
            {
                result.AppendLine($"A new personal best!");
            }
            else
            {
                result.AppendLine($"(Biggest fish caught so far: {fisher.BiggestFish} cm)");
            }

            result.AppendLine($"Fish caught by <@{fisher.DiscordUserId}>: {fisher.FishCaught + 1}");
        }
        else
        {
            result.AppendLine("It got away...");
        }

        return result.ToString();
    }

    public List<DiscordComponent> GetComponents()
    {
        return Event switch
        {
            Event.FoundTreasureChest =>
            [
                new DiscordComponent
                {
                    Type = DiscordMessageComponentType.ActionRow,
                    Components =
                    [
                        new DiscordComponent
                        {
                            Type = DiscordMessageComponentType.Button,
                            CustomId = $"open_{_userId}_{PromptId}",
                            Label = "Open it!",
                            Style = DiscordButtonStyleType.Success
                        },
                        new DiscordComponent
                        {
                            Type = DiscordMessageComponentType.Button,
                            CustomId = $"close_{_userId}_{PromptId}",
                            Label = "NO!",
                            Style = DiscordButtonStyleType.Danger
                        }
                    ]
                }
            ],
            _ => [],
        };
    }
}
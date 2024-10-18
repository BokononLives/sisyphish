using Microsoft.AspNetCore.Mvc;
using sisyphish.Tools.Discord.Core.Models;

namespace sisyphish.Tools.Discord;

public static class ExtensionMethods
{
    public static IActionResult From(this ControllerBase controller, IDiscordInteractionResponse response)
    {
        switch (response.ResponseType)
        {
            case DiscordInteractionResponseType.DiscordInteractionResponse:
                return controller.Ok(response);
            case DiscordInteractionResponseType.DiscordInteractionErrorResponse:
                return controller.BadRequest(response);
            default:
                return controller.BadRequest("An unexpected error occurred");
        }
    }
}
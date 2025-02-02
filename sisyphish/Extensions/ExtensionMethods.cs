using Microsoft.AspNetCore.Mvc;
using sisyphish.Discord.Models;

namespace sisyphish.Extensions;

public static class ControllerBaseExtensions
{
    public static IActionResult From(this ControllerBase controller, IDiscordInteractionResponse response)
    {
        switch (response.ResponseType)
        {
            case DiscordInteractionResponseType.DiscordInteractionResponse:
                return controller.Ok(response);
            case DiscordInteractionResponseType.DeferredDiscordInteractionResponse:
                return controller.Accepted();
            case DiscordInteractionResponseType.DiscordInteractionErrorResponse:
                return controller.BadRequest(response);
            default:
                return controller.BadRequest("An unexpected error occurred");
        }
    }
}
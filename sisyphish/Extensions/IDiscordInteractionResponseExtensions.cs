using sisyphish.Discord.Models;

namespace sisyphish.Extensions;

public static class IDiscordInteractionResponseExtensions
{
    public static IResult ToResult(this IDiscordInteractionResponse response)
    {
        switch (response.ResponseType)
        {
            case DiscordInteractionResponseType.DiscordInteractionResponse:
                return Results.Ok(response);
            case DiscordInteractionResponseType.DeferredDiscordInteractionResponse:
                return Results.Accepted();
            case DiscordInteractionResponseType.DiscordInteractionErrorResponse:
                return Results.BadRequest(response);
            default:
                return Results.BadRequest("An unexpected error occurred");
        }
    }
}
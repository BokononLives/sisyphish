using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace sisyphish.Filters;

public class GoogleCloudAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var authHeaderPrefix = "Bearer ";

        var authHeader = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith(authHeaderPrefix, StringComparison.InvariantCultureIgnoreCase) == true)
        {
            var bearerToken = authHeader[authHeaderPrefix.Length..];

            var payload = await GoogleJsonWebSignature.ValidateAsync(bearerToken);

            if (payload.Email.Equals(Config.GoogleServiceAccount))
            {
                await next();
            }
        }
        
        context.Result = new UnauthorizedObjectResult("Invalid request");
    }
}
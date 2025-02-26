using Google.Apis.Auth;

namespace sisyphish.Filters;

public class GoogleCloudFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var authHeaderPrefix = "Bearer ";

        var authHeader = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith(authHeaderPrefix, StringComparison.InvariantCultureIgnoreCase) == true)
        {
            var bearerToken = authHeader[authHeaderPrefix.Length..];

            var payload = await GoogleJsonWebSignature.ValidateAsync(bearerToken);

            if (payload.Email.Equals(Config.GoogleServiceAccount))
            {
                await next(context);
            }
        }

        return Results.Unauthorized();
    }
}
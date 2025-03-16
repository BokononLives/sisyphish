using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace sisyphish.Filters;

public class GoogleCloudFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            var authHeaderPrefix = "Bearer ";

            var authHeader = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();
            if ((authHeader?.StartsWith(authHeaderPrefix, StringComparison.InvariantCultureIgnoreCase)) != true)
            {
                return Results.Unauthorized();
            }

            var bearerToken = authHeader[authHeaderPrefix.Length..];
            var keys = await GetGooglePublicKeys();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = ["accounts.google.com", "https://accounts.google.com"],
                ValidateAudience = true,
                ValidAudience = context.HttpContext.Request.GetEncodedUrl(),
                ValidateLifetime = true,
                IssuerSigningKeys = keys
            };

            var jwtHandler = new JwtSecurityTokenHandler();
            jwtHandler.ValidateToken(bearerToken, validationParameters, out var validatedToken);

            var token = (JwtSecurityToken)validatedToken;
            var emailAddress = token.Claims.First(c => c.Type == "email");

            if (!emailAddress.Equals(Config.GoogleServiceAccount))
            {
                return Results.Unauthorized();
            }
        }
        catch (Exception ex)
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }

    private static async Task<IEnumerable<SecurityKey>> GetGooglePublicKeys()
    {
        using var httpClient = new HttpClient();

        var certsResponse = await httpClient.GetStringAsync(
            requestUri: Config.GoogleCertsBaseUrl
        );

        var result = new JsonWebKeySet(certsResponse).Keys.OfType<SecurityKey>();
        return result;
    }
}
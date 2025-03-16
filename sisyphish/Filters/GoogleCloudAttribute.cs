using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.IdentityModel.Tokens;

namespace sisyphish.Filters;

public class GoogleCloudFilter : IEndpointFilter
{
    private readonly ILogger<GoogleCloudFilter> _logger;

    public GoogleCloudFilter(ILogger<GoogleCloudFilter> logger)
    {
        _logger = logger;
    }

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
                ValidAudiences = [
                    new UriBuilder
                    {
                        Scheme = "http",
                        Host = context.HttpContext.Request.Host.Host,
                        Port = context.HttpContext.Request.Host.Port ?? -1,
                        Path = context.HttpContext.Request.Path,
                        Query = context.HttpContext.Request.QueryString.ToUriComponent()
                    }.ToString(),
                    new UriBuilder
                    {
                        Scheme = "https",
                        Host = context.HttpContext.Request.Host.Host,
                        Port = context.HttpContext.Request.Host.Port ?? -1,
                        Path = context.HttpContext.Request.Path,
                        Query = context.HttpContext.Request.QueryString.ToUriComponent()
                    }.ToString()],
                ValidateLifetime = true,
                IssuerSigningKeys = keys
            };

            var jwtHandler = new JwtSecurityTokenHandler();
            jwtHandler.ValidateToken(bearerToken, validationParameters, out var validatedToken);

            var token = (JwtSecurityToken)validatedToken;
            var emailAddress = token.Claims.First(c => c.Type == "email");

            if (!string.Equals(emailAddress.Value, Config.GoogleServiceAccount, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogInformation(@$"Validation failed:
                    - token email address: {emailAddress}");

                return Results.Unauthorized();
            }

            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed");
        }

        return Results.Unauthorized();
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
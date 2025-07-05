using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using sisyphish.Tools;

namespace sisyphish.Filters;

public class GoogleCloudFilter : IEndpointFilter
{
    private readonly ILogger<GoogleCloudFilter> _logger;
    private readonly HttpClient _httpClient;

    public GoogleCloudFilter(ILogger<GoogleCloudFilter> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(nameof(GoogleCloudFilter));
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

            if (!emailAddress.Value.Equals(Config.GoogleServiceAccount))
            {
                _logger?.LogInformation(@$"Validation failed:
                    - token email address: {emailAddress}");

                return Results.Unauthorized();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Validation failed");

            return Results.Unauthorized();
        }
        
        return await next(context);
    }

    private async Task<IEnumerable<SecurityKey>> GetGooglePublicKeys()
    {
        _logger?.LogInformation($"Attempting to hit BaseUrl={_httpClient.BaseAddress?.AbsoluteUri}, path = v3/certs/");

        var httpResponse = await _httpClient.GetStringAsync(
            requestUri: "v3/certs/"
        );

        var result = new JsonWebKeySet(httpResponse).Keys.OfType<SecurityKey>();
        return result;
    }
}
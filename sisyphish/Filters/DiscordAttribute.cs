using System.Text;
using NSec.Cryptography;

namespace sisyphish.Filters;

public class DiscordFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);

        var requestSignature = context.HttpContext.Request.Headers["X-Signature-Ed25519"].FirstOrDefault() ?? string.Empty;
        var requestTimestamp = context.HttpContext.Request.Headers["X-Signature-Timestamp"].FirstOrDefault() ?? string.Empty;
        var requestBody = await new StreamReader(context.HttpContext.Request.Body).ReadToEndAsync();

        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, GetBytesFromHexString(Config.DiscordPublicKey), KeyBlobFormat.RawPublicKey);
        var verified = SignatureAlgorithm.Ed25519.Verify(publicKey, Encoding.UTF8.GetBytes(requestTimestamp + requestBody), GetBytesFromHexString(requestSignature));

        if (!verified)
        {
            return Results.Unauthorized();
        }

        context.HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
        
        return await next(context);
    }

    private static byte[] GetBytesFromHexString(string hex)
    {
        var length = hex.Length;
        var bytes = new byte[length / 2];

        for (int i = 0; i < length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }

        return bytes;
    }
}
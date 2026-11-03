using System.Text;
using Microsoft.AspNetCore.Http;

namespace GSBC.ChurchApp.Platform.Api.Installs;

public sealed class InstallProofMiddleware(RequestDelegate next)
{
    public const string KeyHeader = "gsbc-install";
    public const string AssertionHeader = "gsbc-assertion";
    private const int MaxSignedBody = 256 * 1024;
    private static readonly object ProofKey = new();

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers[KeyHeader].ToString() is { Length: > 0 } keyId
            && TryBase64(context.Request.Headers[AssertionHeader].ToString()) is { } assertion
            && context.Request.ContentLength is null or <= MaxSignedBody)
        {
            context.Request.EnableBuffering(MaxSignedBody);
            using MemoryStream body = new();
            await context.Request.Body.CopyToAsync(body, context.RequestAborted);
            context.Request.Body.Position = 0;
            context.Items[ProofKey] = new InstallProof(keyId, assertion, ClientData(context.Request.Path, body.ToArray()));
        }

        await next(context);
    }

    public static byte[] ClientData(string methodPath, byte[] body) =>
        [.. Encoding.UTF8.GetBytes(methodPath + "\n"), .. body];

    internal static InstallProof? ProofOf(HttpContext context) => context.Items[ProofKey] as InstallProof;

    private static byte[]? TryBase64(string value)
    {
        if (value.Length == 0)
            return null;
        byte[] buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out int written) ? buffer[..written] : null;
    }
}

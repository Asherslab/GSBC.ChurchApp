using System.Buffers;
using System.IO.Pipelines;
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
            && context.Request.ContentLength is null or <= MaxSignedBody
            && await PeekBodyAsync(context.Request.BodyReader, context.RequestAborted) is { } body)
        {
            context.Items[ProofKey] = new InstallProof(keyId, assertion, ClientData(context.Request.Path, body));
        }

        await next(context);
    }

    private static async Task<byte[]?> PeekBodyAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        while (true)
        {
            ReadResult read = await reader.ReadAsync(cancellationToken);
            ReadOnlySequence<byte> buffer = read.Buffer;
            bool tooLarge = buffer.Length > MaxSignedBody;
            byte[]? body = read.IsCompleted && !tooLarge ? buffer.ToArray() : null;
            reader.AdvanceTo(buffer.Start, buffer.End);
            if (read.IsCompleted || tooLarge)
                return body;
        }
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

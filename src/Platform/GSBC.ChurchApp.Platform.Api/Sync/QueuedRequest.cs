using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Platform.Api.Sync;

public static class QueuedRequest
{
    public const string IdHeader = "gsbc-request-id";
    public const int LongestId = 64;

    public static string? IdOf(CallContext context) =>
        context.RequestHeaders?.GetValue(IdHeader) is { Length: > 0 and <= LongestId } id && !string.IsNullOrWhiteSpace(id) ? id : null;
}

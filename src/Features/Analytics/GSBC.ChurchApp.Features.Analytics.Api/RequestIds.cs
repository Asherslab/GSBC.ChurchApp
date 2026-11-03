using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal static class RequestIds
{
    private const string Header = "gsbc-request-id";

    public static string? Of(CallContext context) =>
        context.RequestHeaders?.GetValue(Header) is { Length: > 0 } id && !string.IsNullOrWhiteSpace(id) ? id : null;
}

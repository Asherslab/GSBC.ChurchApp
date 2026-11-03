using Grpc.Core;
using GSBC.ChurchApp.Features.Analytics.Contracts;

namespace GSBC.ChurchApp.Features.Analytics.Client;

public static class HealthWords
{
    public static string ShortType(string type) => type[(type.LastIndexOf('.') + 1)..];

    public static string ShortFrame(string frame)
    {
        string[] parts = frame.Split('.');
        return parts.Length <= 2 ? frame : $"{parts[^2]}.{parts[^1]}";
    }

    public static string Method(string method) => method.TrimStart('/').Replace("gsbc.", "", StringComparison.Ordinal).Replace('/', '.');

    public static string Origin(ReportOrigin origin) => origin switch
    {
        ReportOrigin.App => "The app",
        ReportOrigin.Admin => "The admin panel",
        ReportOrigin.Server => "The server",
        _ => "Unknown",
    };

    public static string Times(long count) => count == 1 ? "Once" : $"{count} times";

    public static string Status(int status) => (StatusCode)status switch
    {
        StatusCode.Unauthenticated => "Not signed in",
        StatusCode.PermissionDenied => "Not permitted",
        StatusCode.InvalidArgument => "Bad request",
        StatusCode.NotFound => "Not found",
        StatusCode.AlreadyExists => "Already exists",
        StatusCode.FailedPrecondition => "Refused",
        StatusCode.ResourceExhausted => "Too many requests",
        StatusCode.Unavailable => "Couldn't reach the server",
        StatusCode.DeadlineExceeded => "Timed out",
        StatusCode.Unimplemented => "No such call",
        StatusCode.Internal => "Server error",
        StatusCode.Unknown => "Unexpected error",
        StatusCode.DataLoss => "Data lost",
        StatusCode.Aborted => "Aborted",
        StatusCode.OutOfRange => "Out of range",
        _ => $"Status {status}",
    };
}

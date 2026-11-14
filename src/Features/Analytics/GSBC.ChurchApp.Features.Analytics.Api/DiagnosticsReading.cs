using System.Security.Cryptography;
using System.Text;
using Grpc.Core;
using GSBC.ChurchApp.Features.Analytics.Contracts;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed record ErrorSighting(
    ReportOrigin Origin, bool NativeCrash, string ExceptionType, List<string> Frames, string Route, DateOnly Day, int Count,
    DevicePlatform Platform, string AppVersion, string AppBuild, int OsMajor, bool DevelopmentBuild);

internal sealed record FailedCallLine(DateOnly Day, ReportOrigin Origin, string Method, int Status, string AppVersion, int Count);

internal sealed record DiagnosticsLines(IReadOnlyList<ErrorSighting> Sightings, IReadOnlyList<FailedCallLine> FailedCalls);

internal static class DiagnosticsReading
{
    private const int FingerprintFrames = 5;
    private const int MaximumCountPerLine = 1_000;
    private const int MaximumTypeLength = 200;
    private const int MaximumRouteLength = 200;
    private const int MaximumMethodLength = 200;
    private const int MaximumVersionLength = 32;
    private const int OldestDayAccepted = 30;

    public static DiagnosticsLines? Read(DiagnosticsReport report, DateOnly today)
    {
        ReportContext context = report.Context ?? new ReportContext();
        if (context.Origin is not (ReportOrigin.App or ReportOrigin.Admin))
            return null;

        List<ErrorSighting> sightings = [];
        foreach (ErrorOccurrence error in report.Errors.Take(DiagnosticsReport.MaximumErrors))
        {
            if (DayOf(error.Day, today) is not { } day || string.IsNullOrWhiteSpace(error.ExceptionType))
                continue;

            sightings.Add(new ErrorSighting(context.Origin, error.NativeCrash, Clip(error.ExceptionType, MaximumTypeLength),
                CleanFrames(error.Frames), Clip(error.Route, MaximumRouteLength), day, CountOf(error.Count),
                context.Platform, Clip(error.AppVersion ?? context.AppVersion, MaximumVersionLength),
                Clip(error.AppBuild ?? context.AppBuild, MaximumVersionLength), context.OsMajor, context.DevelopmentBuild));
        }

        List<FailedCallLine> calls = [];
        foreach (FailedCallCount call in report.FailedCalls.Take(DiagnosticsReport.MaximumFailedCalls))
        {
            if (DayOf(call.Day, today) is not { } day || !LooksLikeAMethod(call.Method) || !FailedCalls.IsAFailure((StatusCode)call.Status))
                continue;
            calls.Add(new FailedCallLine(day, context.Origin, call.Method, call.Status, Clip(context.AppVersion, MaximumVersionLength), CountOf(call.Count)));
        }

        return new DiagnosticsLines(sightings, calls);
    }

    public static (FailedCallLine Call, ErrorSighting? Sighting) ServerFailure(string method, StatusCode status, Exception? unexpected, DateOnly today)
    {
        FailedCallLine call = new(today, ReportOrigin.Server, Clip(method, MaximumMethodLength), (int)status, "", 1);
        ErrorSighting? sighting = unexpected is null
            ? null
            : new ErrorSighting(ReportOrigin.Server, false, Clip(StackSummary.TypeOf(unexpected), MaximumTypeLength),
                StackSummary.FramesOf(unexpected), Clip(method, MaximumRouteLength), today, 1, DevicePlatform.Unknown, "", "", 0, false);
        return (call, sighting);
    }

    public static string Fingerprint(ErrorSighting sighting)
    {
        string identity = string.Join('\n', [$"{sighting.Origin}", $"{sighting.NativeCrash}", sighting.ExceptionType,
            .. sighting.Frames.Take(FingerprintFrames), sighting.Frames.Count == 0 ? sighting.Route : ""]);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }

    private static DateOnly? DayOf(int dayNumber, DateOnly today)
    {
        if (dayNumber < today.DayNumber - OldestDayAccepted || dayNumber > today.DayNumber + 1)
            return null;
        return DateOnly.FromDayNumber(dayNumber);
    }

    private static List<string> CleanFrames(IEnumerable<string> frames) =>
        frames.Where(frame => !string.IsNullOrWhiteSpace(frame))
            .Take(StackSummary.MaximumFrames)
            .Select(frame => Clip(frame, StackSummary.MaximumFrameLength))
            .ToList();

    private static bool LooksLikeAMethod(string method) =>
        method.Length <= MaximumMethodLength && method.StartsWith('/') && method.Count(character => character == '/') == 2;

    private static int CountOf(int count) => Math.Clamp(count, 1, MaximumCountPerLine);

    private static string Clip(string? text, int length)
    {
        string trimmed = (text ?? "").Trim();
        return trimmed.Length > length ? trimmed[..length] : trimmed;
    }
}

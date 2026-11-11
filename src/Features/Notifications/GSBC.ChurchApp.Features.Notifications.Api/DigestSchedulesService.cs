using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal sealed class DigestSchedulesService(DigestSchedules schedules, IAccessEvaluator access) : IDigestSchedulesService
{
    public const int MostEarlyDigests = 12;
    private static readonly TimeSpan LongestWait = TimeSpan.FromDays(7);

    public async ValueTask<DigestScheduleMessage> GetAsync(DigestScheduleRequest request, CallContext context = default) =>
        await MessageAsync(KindOf(request.Kind), context);

    public async ValueTask<DigestScheduleMessage> ChangeAsync(DigestScheduleChange request, CallContext context = default)
    {
        NotificationKind kind = KindOf(request.Kind);
        if (!await MayChangeAsync(kind, User(context), context.CancellationToken))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "You can't change when these are sent."));
        if (ProblemWith(request) is { } problem)
            throw new RpcException(new Status(StatusCode.InvalidArgument, problem));

        DigestSchedule schedule = new(request.AfterMinutes.Distinct().Order().Select(minutes => TimeSpan.FromMinutes(minutes)).ToList(),
            request.DailyAtMinute is { } minute ? TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minute)) : null);
        await schedules.ChangeAsync(kind, schedule, Caller.UserId(User(context)), context.CancellationToken);
        return await MessageAsync(kind, context);
    }

    public static string? ProblemWith(DigestScheduleChange change) => change switch
    {
        { AfterMinutes.Count: 0, DailyAtMinute: null } => "Choose at least one time to send them.",
        { AfterMinutes.Count: > MostEarlyDigests } => $"Choose at most {MostEarlyDigests} times after it starts.",
        _ when change.AfterMinutes.Any(minutes => minutes < 1 || minutes > LongestWait.TotalMinutes) => "Each time after it starts is between a minute and a week.",
        { DailyAtMinute: < 0 or >= 24 * 60 } => "Choose a time of day.",
        _ => null,
    };

    private async Task<DigestScheduleMessage> MessageAsync(NotificationKind kind, CallContext context)
    {
        DigestSchedule chosen = await schedules.OfAsync(kind, context.CancellationToken);
        DigestSchedule standard = DigestSchedules.DigestOf(kind).Schedule;
        return new DigestScheduleMessage
        {
            Kind = kind.Key,
            AfterMinutes = Minutes(chosen.After),
            DailyAtMinute = Minute(chosen.DailyAt),
            DefaultAfterMinutes = Minutes(standard.After),
            DefaultDailyAtMinute = Minute(standard.DailyAt),
            MayChange = await MayChangeAsync(kind, User(context), context.CancellationToken),
        };
    }

    private NotificationKind KindOf(string key)
    {
        try
        {
            return schedules.Kind(key);
        }
        catch (ArgumentException exception)
        {
            throw new RpcException(new Status(StatusCode.NotFound, exception.Message));
        }
    }

    private async Task<bool> MayChangeAsync(NotificationKind kind, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        kind.Digest?.ChangedBy is { } permission && await access.IsAllowedAsync(user, permission.Node, null, cancellationToken);

    private static List<int> Minutes(IReadOnlyList<TimeSpan> after) => after.Select(each => (int)each.TotalMinutes).ToList();

    private static int? Minute(TimeOnly? at) => at is { } time ? time.Hour * 60 + time.Minute : null;

    private static ClaimsPrincipal User(CallContext context) => context.ServerCallContext!.GetHttpContext().User;
}

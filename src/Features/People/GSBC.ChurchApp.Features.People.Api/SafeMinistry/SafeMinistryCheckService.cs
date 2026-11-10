using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.People.Api.SafeMinistry;

internal sealed class SafeMinistryCheckService(PeopleDbContext db, SafeMinistrySync sync, IPeopleAccounts accounts, TimeProvider time) : ISafeMinistryCheckService
{
    public async ValueTask<SmcSyncStatus> GetStatusAsync(SmcStatusRequest request, CallContext context = default)
    {
        RefuseTools(context);
        return await StatusAsync(context.CancellationToken);
    }

    public async ValueTask<SmcSyncStatus> SyncNowAsync(SmcSyncRequest request, CallContext context = default)
    {
        RefuseTools(context);
        if (!sync.Connected)
            throw Refused("Safe Ministry Check isn't connected yet.");
        if (!await sync.RunAsync(context.CancellationToken))
            throw Refused("A sync is already running. It'll show here when it's done.");
        return await StatusAsync(context.CancellationToken);
    }

    public async ValueTask<SmcPeopleList> GetUnmatchedAsync(SmcUnmatchedRequest request, CallContext context = default)
    {
        RefuseTools(context);
        List<string> linked = await db.Safeguarding.AsNoTracking()
            .Where(entry => entry.Source == SafeguardingSources.SafeMinistryCheck).Select(entry => entry.ExternalId!).ToListAsync(context.CancellationToken);
        List<SmcPersonRecord> people = await db.SmcPeople.AsNoTracking()
            .Where(person => person.Why != null && !linked.Contains(person.SmcId))
            .OrderBy(person => person.Name).ToListAsync(context.CancellationToken);
        return new SmcPeopleList
        {
            People = people.Select(person => new SmcPerson
            {
                SmcId = person.SmcId,
                Name = person.Name,
                Email = person.Email ?? "",
                BirthDate = WireDates.ToWire(person.BirthDate),
                Why = person.Why ?? "",
            }).ToList(),
        };
    }

    public async ValueTask<Done> LinkAsync(SmcLinkRequest request, CallContext context = default)
    {
        RefuseTools(context);
        Guid personId = ParseId(request.PersonId);
        IReadOnlyDictionary<Guid, PersonAccount> found = await accounts.GetAsync([personId], context.CancellationToken);
        if (!found.TryGetValue(personId, out PersonAccount? person) || person.Deleted)
            throw new RpcException(new Status(StatusCode.NotFound, "That person isn't in the app any more."));
        await Changing(() => sync.LinkByHandAsync(personId, request.SmcId.Trim(), Me(context), context.CancellationToken));
        return new Done();
    }

    public async ValueTask<Done> UnlinkAsync(PersonRequest request, CallContext context = default)
    {
        RefuseTools(context);
        await Changing(() => sync.UnlinkAsync(ParseId(request.PersonId), context.CancellationToken));
        return new Done();
    }

    private async Task<SmcSyncStatus> StatusAsync(CancellationToken cancellationToken)
    {
        SmcSyncState state = await sync.StateAsync(cancellationToken);
        bool connected = sync.Connected;
        return new SmcSyncStatus
        {
            Connected = connected,
            LastRunAt = state.LastRunAt,
            LastSucceededAt = state.LastSucceededAt,
            Problem = state.Problem,
            People = state.People,
            Linked = state.Linked,
            ToMatch = state.ToMatch,
            ToCheck = state.ToCheck,
            NextRunAt = connected ? SafeMinistrySchedule.NextRun(time.GetUtcNow().UtcDateTime) : null,
        };
    }

    private static async Task Changing(Func<Task> change)
    {
        try
        {
            await change();
        }
        catch (SmcLinkException problem)
        {
            throw Refused(problem.Message);
        }
    }

    private static void RefuseTools(CallContext context)
    {
        if (Caller.IsTool(User(context)))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Connected tools can't see or change safeguarding."));
    }

    private static ClaimsPrincipal User(CallContext context) => context.ServerCallContext!.GetHttpContext().User;

    private static Guid Me(CallContext context) =>
        Caller.UserId(User(context)) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in again."));

    private static Guid ParseId(string id) =>
        Guid.TryParse(id, out Guid parsed) ? parsed : throw new RpcException(new Status(StatusCode.InvalidArgument, "That isn't a person in the app."));

    private static RpcException Refused(string message) => new(new Status(StatusCode.FailedPrecondition, message));
}

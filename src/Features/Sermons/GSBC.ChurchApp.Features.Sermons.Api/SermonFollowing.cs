using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class SermonFollowing(SermonsDbContext db, IRoleAssignments roles, IPhoneRoles phoneRoles, TimeProvider time)
{
    public async Task<Follows> FollowAsync(Guid installId, Guid? userId, FollowRequest request, CancellationToken cancellationToken)
    {
        string? seriesId = string.IsNullOrEmpty(request.SeriesId) ? null : request.SeriesId;
        if (seriesId is not null)
        {
            if (!Guid.TryParse(seriesId, out Guid id) || !await db.Series.AnyAsync(series => series.Id == id, cancellationToken))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "That series isn't here any more."));
            seriesId = id.ToString();
        }
        string role = seriesId is null ? SermonsPermissions.FollowerRole : SermonsPermissions.SeriesFollowerRole;

        if (userId is { } person)
        {
            bool following = (await roles.HoldersAsync(role, seriesId, cancellationToken)).Any(holder => holder.UserId == person);
            if (request.Follow && !following)
                await roles.GiveAsFeatureAsync(person, role, seriesId, null, cancellationToken);
            else if (!request.Follow && following)
                await roles.TakeAwayAsFeatureAsync(person, role, seriesId, cancellationToken);
            await PublishAsync([person], cancellationToken);
        }
        else if (request.Follow)
            await phoneRoles.GiveAsync(installId, role, seriesId, null, cancellationToken);
        else
            await phoneRoles.TakeAwayAsync(installId, role, seriesId, cancellationToken);

        return await FollowsAsync(installId, userId, cancellationToken);
    }

    public async Task<Follows> FollowsAsync(Guid installId, Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is not { } person)
            return PhoneFollows(await phoneRoles.HeldAsync(installId, cancellationToken));

        bool newSermons = (await roles.HoldersAsync(SermonsPermissions.FollowerRole, null, cancellationToken)).Any(holder => holder.UserId == person);
        List<string> series = [];
        foreach (Guid id in await db.Series.AsNoTracking().Select(record => record.Id).ToListAsync(cancellationToken))
            if ((await roles.HoldersAsync(SermonsPermissions.SeriesFollowerRole, id.ToString(), cancellationToken)).Any(holder => holder.UserId == person))
                series.Add(id.ToString());
        return new Follows { NewSermons = newSermons, SeriesIds = series };
    }

    public async Task PublishAsync(IReadOnlyCollection<Guid>? people, CancellationToken cancellationToken)
    {
        List<string> series = await db.Series.AsNoTracking().Select(record => record.Id.ToString()).ToListAsync(cancellationToken);
        List<Guid> everyone = people?.ToList() ?? [
            .. (await roles.HoldersAsync(SermonsPermissions.FollowerRole, null, cancellationToken)).Select(holder => holder.UserId),
            .. (await roles.HoldingsAsync(SermonsPermissions.SeriesFollowerRole, series, cancellationToken)).Select(holding => holding.UserId)];
        List<string> ids = everyone.Distinct().Select(person => person.ToString()).ToList();
        Dictionary<string, FeedChangeRecord> published = (await db.FeedChanges.AsNoTracking()
                .Where(change => change.Feed == SermonsFeeds.Follows && (people == null || ids.Contains(change.RecordId)))
                .GroupBy(change => change.RecordId)
                .Select(versions => versions.OrderByDescending(change => change.Cursor).First())
                .ToListAsync(cancellationToken))
            .ToDictionary(change => change.RecordId);
        long version = time.GetUtcNow().UtcDateTime.Ticks;
        foreach (Guid person in everyone.Distinct())
        {
            byte[] payload = Encode(await FollowsAsync(Guid.Empty, person, cancellationToken));
            if (published.GetValueOrDefault(person.ToString()) is { Deleted: false } before && before.Payload.AsSpan().SequenceEqual(payload))
                continue;
            db.FeedChanges.Add(new FeedChangeRecord { Feed = SermonsFeeds.Follows, RecordId = person.ToString(), ScopeId = person.ToString(), Version = version, Payload = payload });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static byte[] Encode(Follows follows)
    {
        using MemoryStream stream = new();
        SermonsContractsModel.Instance.Serialize(stream, follows);
        return stream.ToArray();
    }

    internal static Follows PhoneFollows(IReadOnlyList<HeldRole> held) => new()
    {
        NewSermons = held.Any(role => role.RoleKey == SermonsPermissions.FollowerRole),
        SeriesIds = held.Where(role => role is { RoleKey: SermonsPermissions.SeriesFollowerRole, ObjectId: not null }).Select(role => role.ObjectId!).ToList(),
    };
}

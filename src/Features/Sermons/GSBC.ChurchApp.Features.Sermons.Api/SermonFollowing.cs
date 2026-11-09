using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class SermonFollowing(SermonsDbContext db, IRoleAssignments roles, IPhoneRoles phoneRoles)
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
            if (request.Follow)
                await roles.GiveAsFeatureAsync(person, role, seriesId, null, cancellationToken);
            else
                await roles.TakeAwayAsFeatureAsync(person, role, seriesId, cancellationToken);
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

    internal static Follows PhoneFollows(IReadOnlyList<HeldRole> held) => new()
    {
        NewSermons = held.Any(role => role.RoleKey == SermonsPermissions.FollowerRole),
        SeriesIds = held.Where(role => role is { RoleKey: SermonsPermissions.SeriesFollowerRole, ObjectId: not null }).Select(role => role.ObjectId!).ToList(),
    };
}

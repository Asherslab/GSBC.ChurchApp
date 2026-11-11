using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Chats;
using GSBC.ChurchApp.Platform.Contracts.Chats;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryChatPlaces(MinistriesDbContext db) : IChatPlaces
{
    public async ValueTask<IReadOnlyDictionary<Guid, ChatPlace>> PlacesAsync(IReadOnlyCollection<Guid> ministryIds, CancellationToken cancellationToken)
    {
        List<MinistryRecord> ministries = await db.Ministries.AsNoTracking().Where(ministry => ministryIds.Contains(ministry.Id)).ToListAsync(cancellationToken);
        return ministries.ToDictionary(ministry => ministry.Id, Place);
    }

    public async ValueTask<IReadOnlyList<ChatPlace>> AllAsync(CancellationToken cancellationToken) =>
        (await db.Ministries.AsNoTracking().OrderBy(ministry => ministry.Order).ThenBy(ministry => ministry.Name).ToListAsync(cancellationToken))
        .Select(Place)
        .ToList();

    internal static ChatPlace Place(MinistryRecord ministry)
    {
        Scope at = Scope.Of(ScopeKind.Ministry, ministry.Id.ToString());
        Dictionary<string, Audience> audiences = new() { [ChatAudiences.Team] = Audience.HoldingAt(MinistriesPermissions.TeamViewNode, at) };
        if (ministry.AttendJoin != Joining.None)
        {
            audiences[ChatAudiences.Attendees] = Audience.HoldingAt(MinistriesPermissions.AttendeesViewNode, at);
            audiences[ChatAudiences.Parents] = Audience.HoldingAt(MinistriesPermissions.ParentsViewNode, at);
        }
        return new ChatPlace(ministry.Id, ministry.Name, MinistryParts.IsOn(ministry.PartsOff, MinistryParts.Chats), audiences, ministry.ChatStarters == ChatStarters.Team);
    }
}

using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryRemover(MinistriesDbContext db, MinistryChanges changes, IRoleAssignments roles, IMinistryCalendars calendars)
{
    public async Task<DeletionPreview> PreviewAsync(MinistryTree tree, MinistryRecord ministry, CancellationToken cancellationToken)
    {
        DeletionPreview preview = new()
        {
            Name = ministry.Name,
            SubMinistries = tree.ChildrenOf(ministry.Id).Count(),
            SubMinistriesGoTo = ministry.ParentId is { } parent ? tree.Find(parent)?.Name ?? "" : "",
            Posts = await db.Posts.CountAsync(post => post.MinistryId == ministry.Id, cancellationToken),
            Files = await db.Files.CountAsync(file => file.MinistryId == ministry.Id, cancellationToken),
        };
        foreach (string role in new[] { Roles.Leader, Roles.Admin, Roles.Team, Roles.Attendee })
            preview.People += await roles.HoldingCountAsync(Roles.KeyOf(role), ministry.Id.ToString(), cancellationToken);
        foreach (MinistryCalendar calendar in await calendars.OfAsync(ministry.Id, cancellationToken))
        {
            preview.Calendars.Add(calendar.Name);
            MinistryCalendarRemoval removal = await calendars.WhatGoesAsync(ministry.Id, calendar.Id, cancellationToken);
            preview.Events += removal.Events + removal.RegularEvents;
        }
        return preview;
    }

    public async Task RemoveAsync(MinistryTree tree, MinistryRecord ministry, CancellationToken cancellationToken)
    {
        foreach (MinistryCalendar calendar in await calendars.OfAsync(ministry.Id, cancellationToken))
            await calendars.RemoveAsync(ministry.Id, calendar.Id, cancellationToken);

        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            await db.Ministries.Where(child => child.ParentId == ministry.Id).ExecuteUpdateAsync(set => set.SetProperty(child => child.ParentId, ministry.ParentId), cancellationToken);
            await db.Posts.Where(post => post.MinistryId == ministry.Id).ExecuteDeleteAsync(cancellationToken);
            await db.Files.Where(file => file.MinistryId == ministry.Id).ExecuteDeleteAsync(cancellationToken);
            await db.Asks.Where(ask => ask.MinistryId == ministry.Id).ExecuteDeleteAsync(cancellationToken);
            await db.Sessions.Where(session => session.GroupId == ministry.Id).ExecuteDeleteAsync(cancellationToken);
            await db.Ministries.Where(found => found.Id == ministry.Id).ExecuteDeleteAsync(cancellationToken);
            changes.Touch(ministry.Id, deleted: true);
            foreach (MinistryRecord child in tree.ChildrenOf(ministry.Id))
                changes.Touch(child.Id);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await roles.ForgetScopeAsync(ScopeKind.Ministry, [ministry.Id.ToString()], cancellationToken);
        await roles.ScopesChangedAsync(cancellationToken);
    }
}

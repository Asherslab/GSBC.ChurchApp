using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Platform.Api.Calendars;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryCalendarShaper(MinistriesDbContext db, MinistryViews views, IMinistryCalendars calendars)
{
    public async Task ShapeAsync(Guid ministryId, CancellationToken cancellationToken)
    {
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        if (await db.Ministries.AsNoTracking().SingleOrDefaultAsync(found => found.Id == ministryId, cancellationToken) is not { } ministry)
            return;
        MinistryCalendarShape shape = MinistryCalendarPlan.Shape(ministry.Id, ministry.Name, tree.IsHidden(ministry.Id), await calendars.OfAsync(ministry.Id, cancellationToken));
        foreach (CalendarMerge merge in shape.Merges)
            await calendars.MergeAsync(ministry.Id, merge.From, merge.Into, cancellationToken);
        foreach (CalendarFix fix in shape.Fixes)
            await calendars.ChangeAsync(ministry.Id, fix.Id, fix.Name, fix.Audience, cancellationToken);
    }

    public async Task ShapeAllAsync(CancellationToken cancellationToken)
    {
        foreach (Guid id in await db.Ministries.AsNoTracking().Select(ministry => ministry.Id).ToListAsync(cancellationToken))
            await ShapeAsync(id, cancellationToken);
    }
}

internal sealed class MinistryCalendarShaping(IServiceScopeFactory scopes, ILogger<MinistryCalendarShaping> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<MinistryCalendarShaper>().ShapeAllAsync(stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Couldn't bring the ministries' calendars to one Public and one Team calendar each");
        }
    }
}

internal sealed class LinkedCalendarMerges(MinistriesDbContext db, MinistryChanges changes) : ICalendarMerges
{
    public async ValueTask MergedAsync(Guid fromCalendarId, Guid intoCalendarId, CancellationToken cancellationToken)
    {
        foreach (MinistryRecord ministry in await db.Ministries.Where(ministry => ministry.LinkedCalendars.Contains(fromCalendarId)).ToListAsync(cancellationToken))
        {
            ministry.LinkedCalendars = ministry.LinkedCalendars.Select(id => id == fromCalendarId ? intoCalendarId : id).Distinct().ToList();
            changes.Touch(ministry.Id);
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}

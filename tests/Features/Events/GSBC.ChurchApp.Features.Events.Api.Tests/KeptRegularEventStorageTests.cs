using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

[Integration("Keeping a regular event in step replaces its dates, specials and breaks in Postgres in one transaction, and removing it deletes its rows and tells phones")]
public sealed class KeptRegularEventStorageTests : EventsHost
{
    private static readonly TimeOnly Nine = new(9, 0), QuarterToTen = new(9, 45);

    [Fact]
    public async Task ItFollowsEveryChangeMovesWithItsCalendarAndGoesWhenRemoved()
    {
        CalendarSettings services = await CalendarAsync("Services", Audience.Everyone);
        CalendarSettings whole = await CalendarAsync("Whole church", Audience.Everyone);
        System.Security.Claims.ClaimsPrincipal planner = Person((EventsPermissions.EditNode, services.Calendar.Id), (EventsPermissions.EditNode, whole.Calendar.Id));

        await KeptAsync(kept => kept.PutInAsync(planner, Guid.Parse(services.Calendar.Id),
            Plan([On(1), On(2), new KeptDate(Sunday(5), new TimeOnly(10, 0), new TimeOnly(10, 45))], [new KeptGap(Sunday(3), "Between groups"), new KeptGap(Sunday(4), "Between groups")]), Cancel));
        Assert.Equal([Sunday(1), Sunday(2), Sunday(3), Sunday(4), Sunday(5)], await DatesAsync());
        Assert.Equal([Sunday(3), Sunday(4)], await DatesAsync(notOn: true));

        await KeptAsync(kept => kept.KeepInStepAsync(Plan([On(1), On(2), On(3)], []), Cancel));
        Assert.Equal([Sunday(1), Sunday(2), Sunday(3)], await DatesAsync());
        Assert.Empty(await DatesAsync(notOn: true));
        Assert.Equal((0, 0), await WithAsync(async provider =>
        {
            EventsDbContext db = provider.GetRequiredService<EventsDbContext>();
            return (await db.Specials.CountAsync(Cancel), await db.Breaks.CountAsync(Cancel));
        }));

        RegularEventRecord kept = await WithAsync(provider => provider.GetRequiredService<EventsDbContext>().RegularEvents.AsNoTracking().SingleAsync(Cancel));
        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => EditingAsync(planner, (editing, context) => editing.SaveDateAsync(new DateSave
        {
            CalendarId = services.Calendar.Id, RegularEventId = kept.Id.ToString(), Date = EventDates.Format(Sunday(2)), NotOn = true,
        }, context)));
        Assert.Equal(StatusCode.FailedPrecondition, refused.StatusCode);

        await KeptAsync(kept => kept.PutInAsync(planner, Guid.Parse(whole.Calendar.Id), Plan([On(1), On(2), On(3)], []), Cancel));
        Assert.Equal("Whole church", (await KeptAsync(kept => kept.WhereAsync("tests.kept", Cancel)))!.Name);
        Assert.All(await OccurrencesAsync(EventsFeeds.Public), date => Assert.Equal(whole.Calendar.Id, date.CalendarId));

        await KeptAsync(kept => kept.RemoveAsync("tests.kept", Cancel));
        Assert.Empty(await OccurrencesAsync(EventsFeeds.Public));
        Assert.Null(await KeptAsync(kept => kept.WhereAsync("tests.kept", Cancel)));
    }

    [Fact]
    public async Task OnlyACalendarYouEditCanHoldIt()
    {
        CalendarSettings services = await CalendarAsync("Services", Audience.Everyone);
        System.Security.Claims.ClaimsPrincipal planner = Person();

        Assert.Empty(await KeptAsync(kept => kept.ChoicesAsync(planner, Cancel)));
        RpcException refused = await Assert.ThrowsAsync<RpcException>(() =>
            KeptAsync(kept => kept.PutInAsync(planner, Guid.Parse(services.Calendar.Id), Plan([On(1)], []), Cancel)));
        Assert.Equal(StatusCode.PermissionDenied, refused.StatusCode);
    }

    private Task<T> KeptAsync<T>(Func<KeptRegularEvents, ValueTask<T>> work) =>
        WithAsync(async provider => await work(ActivatorUtilities.CreateInstance<KeptRegularEvents>(provider)));

    private Task KeptAsync(Func<KeptRegularEvents, ValueTask> work) =>
        KeptAsync(async kept =>
        {
            await work(kept);
            return true;
        });

    private async Task<List<DateOnly>> DatesAsync(bool? notOn = null) =>
        (await OccurrencesAsync(EventsFeeds.Public)).Where(date => notOn is null || date.NotOn == notOn).Select(date => EventDates.Parse(date.Date)!.Value).ToList();

    private static KeptRegularEvent Plan(IReadOnlyList<KeptDate> on, IReadOnlyList<KeptGap> notOn) =>
        new("tests.kept", "Life Groups", "/life-groups", "Life Groups", "", on, notOn);

    private static KeptDate On(int week) => new(Sunday(week), Nine, QuarterToTen);

    private static DateOnly Sunday(int week) => Today.AddDays(7 * week);
}

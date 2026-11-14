using System.Security.Claims;
using GSBC.ChurchApp.Features.Rosters.Api.Data;
using GSBC.ChurchApp.Features.Rosters.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Api.Ministries;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Testing;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Rosters.Api.Tests;

[Integration("A roster's changes are saved with each person's Sync rows in one transaction, and what each phone's row holds follows the ministry's settings")]
public sealed class RosterFlowTests : IAsyncLifetime
{
    private const string Sunday = "sunday-morning";
    private readonly TestClock _clock = new();
    private readonly FakeMinistries _ministries = new();
    private readonly FakeFamilies _families = new();
    private readonly FakePeople _people = new();
    private readonly FakeEvents _events = new();
    private readonly SentNotifications _notifications = new();
    private readonly Guid _ministry = Guid.CreateVersion7();
    private readonly Guid _leader = Guid.CreateVersion7(), _jo = Guid.CreateVersion7(), _sam = Guid.CreateVersion7(), _tom = Guid.CreateVersion7();
    private readonly Guid _sarah = Guid.CreateVersion7(), _stranger = Guid.CreateVersion7(), _office = Guid.CreateVersion7();
    private readonly Guid _sound = Guid.CreateVersion7(), _date = Guid.CreateVersion7();
    private TestDatabase _database = null!;
    private ServiceProvider _services = null!;

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        _ministries.Ministries[_ministry] = new MinistryFacts(_ministry, "Production", Archived: false, []);
        _ministries.Teams[_ministry] =
        [
            new MinistryMember(_leader, Leads: true), new MinistryMember(_jo, false), new MinistryMember(_sam, false), new MinistryMember(_tom, false),
        ];
        _families.ParentOf[_tom] = _sarah;
        foreach ((Guid id, string name) in new[] { (_leader, "Lee Sample"), (_jo, "Jo Sample"), (_sam, "Sam Sample"), (_tom, "Tom Sample"), (_sarah, "Sarah Sample") })
            _people.Names[id] = name;
        _events.Events[Sunday] = new RosterableEvent(Sunday, "Morning service", Guid.CreateVersion7(), "Sundays", _clock.UtcNow.AddDays(7), _clock.UtcNow.AddDays(7).AddHours(2),
            AllDay: false, NotOn: false);

        ServiceCollection services = new();
        services
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _database.ConnectionString)])
                .Build())
            .AddSingleton<TimeProvider>(_clock)
            .AddLogging()
            .AddSingleton<IMinistryFacts>(_ministries)
            .AddSingleton<IFamilies>(_families)
            .AddSingleton<IPeopleIndex>(_people)
            .AddSingleton<ISafeguardingStandings>(new NobodyCarded())
            .AddSingleton<IRosterableEvents>(_events)
            .AddSingleton<IMinistryCalendars>(new NoCalendars())
            .AddSingleton<INotifications>(_notifications)
            .AddSingleton<IAccessEvaluator>(new LeadersOf(_ministry, _leader, _office))
            .AddScoped<RosterViews>().AddScoped<RosterPublisher>().AddScoped<RosterMessenger>().AddScoped<RosterReminders>()
            .AddScoped<RostersService>().AddScoped<RostersPersonData>().AddScoped<RostersMinistryPeople>()
            .AddRostersData()
            .AddSyncData();
        _services = services.BuildServiceProvider();
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync(Cancel);
        await scope.ServiceProvider.GetRequiredService<RostersDbContext>().Database.MigrateAsync(Cancel);
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task ALeaderCanPutSomeoneOnWhoIsAwayAndUncardedAndSeesItFlagged()
    {
        await SetUpAsync(new Position { Name = "Sound", NeedsBlueCard = true });
        DateOnly day = ChurchTime.DayOf(_events.Events[Sunday].StartsAt);
        await Run(service => service.SaveAwayAsync(_jo, new AwaySave { AwayId = Guid.CreateVersion7().ToString(), From = Wire(day), To = Wire(day) }, Cancel));

        await AssignAsync(_jo);

        RosterView leader = await ViewAsync(_leader);
        Assert.Equal([AwayRules.AwayFlag, RosterRules.NoBlueCardFlag], leader.Dates.Single().Assignments.Single().Flags);
        Assert.Empty((await ViewAsync(_jo)).Dates.Single().Assignments.Single().Flags);
        Assert.Contains(_notifications.Sent, sent => sent.Audience == NotificationAudience.Person(_jo) && sent.Title.StartsWith("You're on", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AParentAnswersForTheirChildAndSomeoneElseCant()
    {
        await SetUpAsync(new Position { Name = "Sound" });
        await AssignAsync(_tom);
        Guid assignment = await OnlyAssignmentAsync();

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() =>
            Run(service => service.ReplyAsync(_stranger, new ReplyRequest { MinistryId = _ministry.ToString(), AssignmentId = assignment.ToString(), Accept = true }, Cancel)));
        Assert.Equal(StatusCode.FailedPrecondition, refused.StatusCode);

        await Run(service => service.ReplyAsync(_sarah, new ReplyRequest { MinistryId = _ministry.ToString(), AssignmentId = assignment.ToString(), Accept = true }, Cancel));

        Assignment answered = (await ViewAsync(_sarah)).Dates.Single().Assignments.Single();
        Assert.Equal((Replies.Accepted, "Sarah, for Tom"), (answered.State, answered.RepliedBy));
        Assert.Contains(_notifications.Sent, sent => sent.Audience == NotificationAudience.Person(_sarah) && sent.Title.StartsWith("For Tom: ", StringComparison.Ordinal));
        Assert.DoesNotContain(_notifications.Sent, sent => sent.Audience == NotificationAudience.Person(_tom));
    }

    [Fact]
    public async Task EachPhoneGetsOnlyWhatTheMinistryLetsItsTeamSee()
    {
        await SetUpAsync(new Position { Name = "Sound", Needed = 3 }, new RosterSettings { WhoSees = WhoSees.Yours, OpenSwaps = OpenSwaps.Team });
        await AssignAsync(_jo);
        await AssignAsync(_tom);

        Assert.Equal(["Jo Sample"], (await ViewAsync(_jo)).Dates.Single().Assignments.Select(each => each.Name));
        Assert.Equal(["Tom Sample"], (await ViewAsync(_sarah)).Dates.Single().Assignments.Select(each => each.Name));
        Assert.Empty((await ViewAsync(_sam)).Dates);
        Assert.Equal(2, (await ViewAsync(_leader)).Dates.Single().Assignments.Count);
        Assert.Null(await RowAsync(_stranger));
    }

    [Fact]
    public async Task AnApprovedSwapMovesThePlaceOnlyWhenALeaderSaysYes()
    {
        await SetUpAsync(new Position { Name = "Sound" }, new RosterSettings { OpenSwaps = OpenSwaps.Approve });
        await AssignAsync(_jo);
        Guid assignment = await OnlyAssignmentAsync();
        Guid swap = Guid.CreateVersion7();

        await Assert.ThrowsAsync<RpcException>(() => Run(service => service.AskSwapAsync(_jo,
            new SwapAsk { MinistryId = _ministry.ToString(), SwapId = Guid.CreateVersion7().ToString(), AssignmentId = assignment.ToString(), Way = SwapWays.Direct }, Cancel)));
        await Run(service => service.AskSwapAsync(_jo,
            new SwapAsk { MinistryId = _ministry.ToString(), SwapId = swap.ToString(), AssignmentId = assignment.ToString(), Way = SwapWays.Team }, Cancel));
        await Run(service => service.TakeSwapAsync(_sam, new SwapTake { MinistryId = _ministry.ToString(), SwapId = swap.ToString() }, Cancel));

        Assert.Equal("Jo Sample", (await ViewAsync(_leader)).Dates.Single().Assignments.Single().Name);

        await Run(service => service.DecideSwapAsync(_leader, new SwapDecision { MinistryId = _ministry.ToString(), SwapId = swap.ToString(), Yes = true }, Cancel));

        Assignment after = (await ViewAsync(_leader)).Dates.Single().Assignments.Single();
        Assert.Equal(("Sam Sample", Replies.Accepted), (after.Name, after.State));
    }

    [Fact]
    public async Task AnArchivedMinistrysRosterCantChangeAndSendsNoReminders()
    {
        await SetUpAsync(new Position { Name = "Sound" });
        await AssignAsync(_jo);
        Guid assignment = await OnlyAssignmentAsync();
        _ministries.Ministries[_ministry] = _ministries.Ministries[_ministry] with { Archived = true };
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<RosterPublisher>().PublishAllAsync(Cancel);
        _notifications.Sent.Clear();

        await Assert.ThrowsAsync<RpcException>(() => AssignAsync(_sam));
        await Assert.ThrowsAsync<RpcException>(() =>
            Run(service => service.ReplyAsync(_jo, new ReplyRequest { MinistryId = _ministry.ToString(), AssignmentId = assignment.ToString(), Accept = true }, Cancel)));
        _clock.Advance(TimeSpan.FromDays(6));
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<RosterReminders>().SendAsync(Cancel);

        Assert.Empty(_notifications.Sent);
        Assert.False((await ViewAsync(_leader)).MayEdit);
    }

    [Fact]
    public async Task EachReminderIsSentOnceHoweverOftenTheScheduleRuns()
    {
        await SetUpAsync(new Position { Name = "Sound" });
        await AssignAsync(_jo);
        _notifications.Sent.Clear();
        _clock.Advance(TimeSpan.FromDays(6));

        for (int run = 0; run < 3; run++)
            await using (AsyncServiceScope scope = _services.CreateAsyncScope())
                await scope.ServiceProvider.GetRequiredService<RosterReminders>().SendAsync(Cancel);

        Assert.NotEmpty(_notifications.Sent);
        Assert.All(_notifications.Sent.GroupBy(sent => (sent.Kind, sent.Subject)), group => Assert.Single(group));
    }

    [Fact]
    public async Task ForgettingSomeoneTakesThemOffDatesToComeAndLeavesPastOnesFilledBySomeoneWhoHasLeft()
    {
        await SetUpAsync(new Position { Name = "Sound", Needed = 2 });
        await AssignAsync(_jo);
        await AssignAsync(_sam);
        _clock.Advance(TimeSpan.FromDays(8));
        _events.Events["next-sunday"] = _events.Events[Sunday] with { Key = "next-sunday", StartsAt = _clock.UtcNow.AddDays(7), EndsAt = _clock.UtcNow.AddDays(7).AddHours(2) };
        Guid next = Guid.CreateVersion7();
        await Run(service => service.AddDateAsync(_leader, new DateAdd { MinistryId = _ministry.ToString(), DateId = next.ToString(), EventKey = "next-sunday" }, Cancel));
        await Run(service => service.AssignAsync(_leader, new AssignRequest
        {
            MinistryId = _ministry.ToString(), AssignmentId = Guid.CreateVersion7().ToString(), DateId = next.ToString(), PositionId = _sound.ToString(), PersonId = _jo.ToString(),
        }, Cancel));
        _ministries.Teams[_ministry].RemoveAll(member => member.PersonId == _jo);

        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<RostersPersonData>().ForgetAsync(_jo, Cancel);

        RosterView leader = await ViewAsync(_leader);
        Assert.Equal(["Sam Sample", RosterDefaults.FormerMember], leader.Dates.Single(date => date.Id == _date.ToString()).Assignments.Select(each => each.Name).Order());
        Assert.Empty(leader.Dates.Single(date => date.Id == next.ToString()).Assignments);
        Assert.Null(await RowAsync(_jo));
    }

    [Fact]
    public async Task ATraineeIsOnWithSomeoneTrainedAndLeadersHearWhenTheyreLeftWithNobody()
    {
        await SetUpAsync(new Position { Name = "Presenter PC", Needed = 1, NeedsTrained = true });
        await Run(service => service.SetTrainedAsync(_leader, new TrainedChange { MinistryId = _ministry.ToString(), PositionId = _sound.ToString(), PersonId = _jo.ToString(), Trained = true }, Cancel));
        await AssignAsync(_sam);

        Assignment alone = (await ViewAsync(_leader)).Dates.Single().Assignments.Single();
        Assert.Equal((true, false), (alone.Training, alone.Fills));
        Assert.Equal([Training.AloneFlag], alone.Flags);
        Assert.Contains(_notifications.Sent, sent => sent.Audience == NotificationAudience.Person(_sam) && sent.Title.EndsWith(": Presenter PC (training)", StringComparison.Ordinal));

        await AssignAsync(_jo);
        RosterDate partnered = (await ViewAsync(_leader)).Dates.Single();
        Assert.Empty(partnered.Assignments.Single(each => each.PersonId == _sam.ToString()).Flags);
        Assert.Equal(1, partnered.Assignments.Count(each => each.Fills));

        Guid jos = Guid.Parse(partnered.Assignments.Single(each => each.PersonId == _jo.ToString()).Id);
        _notifications.Sent.Clear();
        await Run(service => service.ReplyAsync(_jo, new ReplyRequest { MinistryId = _ministry.ToString(), AssignmentId = jos.ToString(), Accept = false }, Cancel));

        Assert.Contains(_notifications.Sent, sent => sent.Audience == NotificationAudience.Person(_leader) && sent.Title.StartsWith("Sam is training on", StringComparison.Ordinal));
        Assert.Equal([Training.AloneFlag], (await ViewAsync(_leader)).Dates.Single().Assignments.Single(each => each.PersonId == _sam.ToString()).Flags);
    }

    [Fact]
    public async Task AddingADateStartsWithTheCalendarsTheMinistryRostersFrom()
    {
        RosterableEvent sunday = _events.Events[Sunday];
        _events.Events["kids-night"] = sunday with { Key = "kids-night", Title = "Kids night", CalendarId = Guid.CreateVersion7(), CalendarName = "Kids" };
        await Run(service => service.SaveSettingsAsync(_leader, new SettingsSave
        {
            MinistryId = _ministry.ToString(), Settings = new RosterSettings { Calendars = [sunday.CalendarId.ToString(), "not a calendar"] },
        }, Cancel));

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        EventChoices choices = await scope.ServiceProvider.GetRequiredService<RostersService>().EventChoicesAsync(_leader, new RosterQuery { MinistryId = _ministry.ToString() }, Cancel);

        Assert.Equal([(Sunday, true), ("kids-night", false)], choices.Events.OrderBy(choice => choice.Title).Select(choice => (choice.Key, choice.Own)).Reverse());
        Assert.Equal([sunday.CalendarId.ToString()], (await ViewAsync(_leader)).Settings.Calendars);
    }

    [Fact]
    public async Task DaysAwayAreThePersonsAndOnlyTheyTheirParentALeaderOfTheirTeamOrTheOfficeSetThem()
    {
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<RosterPublisher>().PublishAllAsync(Cancel);
        Assert.Equal([_jo.ToString()], (await AwayRowAsync(_jo))!.People.Select(person => person.PersonId));
        Assert.Equal([_sarah.ToString(), _tom.ToString()], (await AwayRowAsync(_sarah))!.People.Select(person => person.PersonId));

        string day = Wire(ChurchTime.Today(_clock).AddDays(3));
        await Assert.ThrowsAsync<RpcException>(() =>
            Run(service => service.SaveAwayAsync(_stranger, new AwaySave { AwayId = Guid.CreateVersion7().ToString(), PersonId = _jo.ToString(), From = day, To = day }, Cancel)));
        await Run(service => service.SaveAwayAsync(_office, new AwaySave { AwayId = Guid.CreateVersion7().ToString(), PersonId = _jo.ToString(), From = day, To = day }, Cancel));
        await Run(service => service.SaveAwayAsync(_leader, new AwaySave
        {
            AwayId = Guid.CreateVersion7().ToString(), PersonId = _jo.ToString(), From = day, Weekly = true, Weekdays = [(int)DayOfWeek.Sunday], EndTime = "12:00",
        }, Cancel));

        Assert.Equal(2, (await AwayRowAsync(_jo))!.People.Single().Spans.Count);
    }

    [Fact]
    public async Task SomeoneAddedToTheTeamHasTheRosterAsSoonAsTheMinistrySaysItsPeopleChanged()
    {
        await SetUpAsync(new Position { Name = "Sound", NeedsTrained = true });
        _ministries.Teams[_ministry].Add(new MinistryMember(_stranger, false));
        Assert.Null(await RowAsync(_stranger));

        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<RostersMinistryPeople>().ChangedAsync([_ministry], Cancel);

        Assert.NotNull(await RowAsync(_stranger));
        Assert.Contains((await ViewAsync(_leader)).Team, member => member.PersonId == _stranger.ToString());
    }

    private async Task SetUpAsync(Position position, RosterSettings? settings = null)
    {
        position.Id = _sound.ToString();
        await Run(service => service.SavePositionAsync(_leader, new PositionSave { MinistryId = _ministry.ToString(), Position = position }, Cancel));
        if (settings is not null)
            await Run(service => service.SaveSettingsAsync(_leader, new SettingsSave { MinistryId = _ministry.ToString(), Settings = settings }, Cancel));
        await Run(service => service.AddDateAsync(_leader, new DateAdd { MinistryId = _ministry.ToString(), DateId = _date.ToString(), EventKey = Sunday }, Cancel));
    }

    private Task AssignAsync(Guid person) => Run(service => service.AssignAsync(_leader, new AssignRequest
    {
        MinistryId = _ministry.ToString(), AssignmentId = Guid.CreateVersion7().ToString(), DateId = _date.ToString(), PositionId = _sound.ToString(), PersonId = person.ToString(),
    }, Cancel));

    private async Task Run(Func<RostersService, ValueTask<Done>> call)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await call(scope.ServiceProvider.GetRequiredService<RostersService>());
    }

    private async Task<Guid> OnlyAssignmentAsync()
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<RostersDbContext>().Assignments.Select(assignment => assignment.Id).SingleAsync(Cancel);
    }

    private async Task<RosterView> ViewAsync(Guid person) => await RowAsync(person) ?? throw new InvalidOperationException("No roster row reached that phone.");

    private async Task<RosterView?> RowAsync(Guid person)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        FeedChangeRecord? row = await scope.ServiceProvider.GetRequiredService<RostersDbContext>().FeedChanges.AsNoTracking()
            .Where(change => change.Feed == RostersFeeds.Ministries && change.ScopeId == person.ToString())
            .OrderByDescending(change => change.Cursor)
            .FirstOrDefaultAsync(Cancel);
        return row is null || row.Deleted ? null : RostersPayloads.Decode<RosterView>(row.Payload);
    }

    private async Task<AwayList?> AwayRowAsync(Guid person)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        FeedChangeRecord? row = await scope.ServiceProvider.GetRequiredService<RostersDbContext>().FeedChanges.AsNoTracking()
            .Where(change => change.Feed == RostersFeeds.Away && change.ScopeId == person.ToString())
            .OrderByDescending(change => change.Cursor)
            .FirstOrDefaultAsync(Cancel);
        return row is null || row.Deleted ? null : RostersPayloads.Decode<AwayList>(row.Payload);
    }

    private static string Wire(DateOnly day) => RosterBook.Wire(day);

    private sealed class LeadersOf(Guid ministry, Guid leader, Guid office) : IAccessEvaluator
    {
        public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) => ValueTask.FromResult(false);

        public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult(
            userId == leader ? new GrantSet([new Grant(PermissionPattern.Parse(RostersPermissions.EditNode), Scope.Of(ScopeKind.Ministry, ministry.ToString()))])
            : userId == office ? new GrantSet([new Grant(PermissionPattern.Parse(RostersPermissions.AwayNode), Scope.Global)])
            : GrantSet.Empty);
    }
}

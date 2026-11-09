using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

[Integration("A scheduled publish is kept on the draft's row and done by the server when the clock reaches it, whoever has the admin panel open")]
public sealed class ScheduledPublishTests : ConfigurationHost
{
    private static readonly string Title = HubSurfaces.LinkTile.Title.Key;
    private static readonly DateTime NewYear = new(2026, 12, 31, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AScheduledDraftPublishesWhatItHoldsAtItsTimeAndNotBefore()
    {
        ClaimsPrincipal publisher = Access.Person(ArrangesHub, PublishesHub);
        Draft draft = await NewDraftAsync(publisher, "2027 theme");
        PatchMessage camp = LinkTile("Camp 2027");
        await SaveAsync(publisher, draft, camp);
        Draft scheduled = await ScheduleAsync(publisher, draft, NewYear);
        Assert.Equal(NewYear, scheduled.PublishAt);

        camp.Values.Single(value => value.Key == Title).Value = "Camp 2027: book now";
        await SaveAsync(publisher, draft, camp);
        Clock.Advance(NewYear - Clock.GetUtcNow().UtcDateTime - TimeSpan.FromMinutes(1));
        await PublishDueAsync();
        Assert.Empty(await LiveAsync(publisher, HubSurfaces.Area));

        Clock.Advance(TimeSpan.FromMinutes(1));
        await PublishDueAsync();

        Placement published = Assert.Single(await LiveAsync(publisher, HubSurfaces.Area));
        Assert.Equal("Camp 2027: book now", ValueOf(published, Title));
        Assert.Equal(FakeAccess.IdOf(publisher).ToString(), published.UpdatedBy);
        Assert.Empty((await Drafts(editor => editor.ListAsync(publisher, Cancel))).Drafts);
    }

    [Fact]
    public async Task ADraftChangedForEveryoneSinceStaysUnpublishedWithTheReasonAndTellsWhoScheduledIt()
    {
        ClaimsPrincipal publisher = Access.Person(ArrangesHub, PublishesHub);
        Placement bible = Assert.Single(await PublishNowAsync(publisher, HubSurfaces.Area, null, LinkTile("Bible")));
        Draft draft = await NewDraftAsync(publisher, "2027 theme");
        await SaveAsync(publisher, draft, Change(bible, (Title, "Read the Bible in 2027")));
        await ScheduleAsync(publisher, draft, NewYear);
        await PublishNowAsync(publisher, HubSurfaces.Area, null, Change(bible, (Title, "The Bible")));

        Clock.Advance(NewYear - Clock.GetUtcNow().UtcDateTime);
        await PublishDueAsync();

        Assert.Equal("The Bible", ValueOf(Assert.Single(await LiveAsync(publisher, HubSurfaces.Area)), Title));
        Draft kept = await GetAsync(publisher, draft);
        Assert.Equal(NewYear, kept.PublishAt);
        Assert.Contains("changed for everyone since", kept.NotPublished);
        Assert.Equal(DraftNotifications.NotPublished, Assert.Single(Notifications.Raised, raised => raised.Kind == DraftNotifications.NotPublished).Kind);

        Clock.Advance(TimeSpan.FromMinutes(5));
        await PublishDueAsync();
        Assert.Single(Notifications.Raised, raised => raised.Kind == DraftNotifications.NotPublished);
    }

    [Fact]
    public async Task ItPublishesOnlyWhileWhoeverScheduledItStillMayPublish()
    {
        ClaimsPrincipal publisher = Access.Person(ArrangesHub, PublishesHub);
        Draft draft = await NewDraftAsync(publisher, "2027 theme");
        await SaveAsync(publisher, draft, LinkTile("Camp 2027"));
        await ScheduleAsync(publisher, draft, NewYear);
        Access.TakeAway(publisher, HubSurfaces.PublishNode);

        Clock.Advance(NewYear - Clock.GetUtcNow().UtcDateTime);
        await PublishDueAsync();

        Assert.Empty(await LiveAsync(publisher, HubSurfaces.Area));
        Assert.Contains("can no longer publish", (await GetAsync(publisher, draft)).NotPublished);
    }

    [Fact]
    public async Task OnlyAPublisherCanScheduleItOrEditItOnceItIsScheduled()
    {
        ClaimsPrincipal author = Access.Person(ArrangesHub);
        ClaimsPrincipal publisher = Access.Person(ArrangesHub, PublishesHub, SeesEveryDraft);
        Draft draft = await NewDraftAsync(author, "2027 theme");
        await SaveAsync(author, draft, LinkTile("Camp 2027"));

        await Assert.ThrowsAsync<RpcException>(() => ScheduleAsync(author, draft, NewYear));
        await ScheduleAsync(publisher, draft, NewYear);

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => SaveAsync(author, draft, LinkTile("Something else")));
        Assert.Equal(StatusCode.PermissionDenied, refused.StatusCode);
        await Drafts(editor => editor.CancelScheduledPublishAsync(author, new DraftQuery { DraftId = draft.Id }, Cancel));
        Clock.Advance(NewYear - Clock.GetUtcNow().UtcDateTime);
        await PublishDueAsync();
        Assert.Empty(await LiveAsync(publisher, HubSurfaces.Area));
    }

    private Task<Draft> ScheduleAsync(ClaimsPrincipal caller, Draft draft, DateTime at) =>
        Drafts(editor => editor.SchedulePublishAsync(caller, new SchedulePublishRequest { DraftId = draft.Id, At = at }, Cancel));

    private Task<Draft> GetAsync(ClaimsPrincipal caller, Draft draft) =>
        Drafts(editor => editor.GetAsync(caller, new DraftQuery { DraftId = draft.Id }, Cancel));

    private Task<IReadOnlyList<DraftEditor.ScheduledOutcome>> PublishDueAsync() =>
        Drafts(editor => editor.PublishDueAsync(Cancel));
}

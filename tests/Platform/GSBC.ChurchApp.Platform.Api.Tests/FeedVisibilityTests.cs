using System.Security.Claims;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Tests;

public sealed class FeedVisibilityTests
{
    private static readonly Permission SeeLibrary = new("media.view", ScopeKind.Global, "See the media library");
    private static readonly Permission ReadNews = new("ministries.news.read", ScopeKind.Ministry, "Read news");
    private static readonly ClaimsPrincipal SignedOut = new(new ClaimsIdentity());

    private readonly Permitted _access = new();

    [Fact]
    public async Task ASignedOutPhoneSeesOnlyPublicFeeds()
    {
        Assert.True(await CanSeeAsync(FeedAudience.Public, SignedOut));
        Assert.False(await CanSeeAsync(FeedAudience.SignedIn, SignedOut));
        Assert.False(await CanSeeAsync(FeedAudience.OwnRecords, SignedOut));
        Assert.False(await CanSeeAsync(FeedAudience.Holding(ReadNews), SignedOut));
        Assert.Empty(await VisibleAsync(FeedAudience.OwnRecords, SignedOut, Row("a", scopeId: null)));
    }

    [Fact]
    public async Task APermissionFeedReachesOnlyThoseWhoHoldItAndOnlyInTheirPlaces()
    {
        ClaimsPrincipal editor = Person(), visitor = Person();
        _access.Allow(editor, SeeLibrary.Node, null);
        _access.Allow(editor, ReadNews.Node, "music");

        Assert.True(await CanSeeAsync(FeedAudience.Holding(SeeLibrary), editor));
        Assert.False(await CanSeeAsync(FeedAudience.Holding(SeeLibrary), visitor));
        Assert.Equal(["music news"], await VisibleAsync(FeedAudience.Holding(ReadNews), editor, Row("music news", "music"), Row("youth news", "youth")));
        Assert.Empty(await VisibleAsync(FeedAudience.Holding(ReadNews), visitor, Row("music news", "music")));
    }

    [Fact]
    public async Task AnOwnRecordsFeedShowsEachPersonOnlyTheirOwnRows()
    {
        ClaimsPrincipal ruth = Person(), naomi = Person();

        List<string> seen = await VisibleAsync(FeedAudience.OwnRecords, ruth, Row("ruth", Caller.UserId(ruth).ToString()), Row("naomi", Caller.UserId(naomi).ToString()));

        Assert.Equal(["ruth"], seen);
    }

    [Fact]
    public async Task AnEachRowFeedShowsARowOnlyToThoseItsAudienceIncludesAndSendsTheRestAsDeletions()
    {
        Guid churchMember = Guid.CreateVersion7();
        ClaimsPrincipal member = Person(), visitor = Person();
        _access.Hold(member, churchMember);
        FeedChangeRecord[] rows = [Row("agm", Audience.HoldingRole(churchMember).Text), Row("helpers night", Audience.SignedIn.Text), Row("no audience", null)];

        Assert.False(await CanSeeAsync(FeedAudience.EachRow, SignedOut));
        Assert.Equal(["agm", "helpers night"], await ShownAsync(member, rows));
        Assert.Equal(["helpers night"], await ShownAsync(visitor, rows));
        List<FeedChangeRecord> hidden = (await FeedVisibility.VisibleRowsAsync(new FeedDefinition("feed", FeedAudience.EachRow), [.. rows], visitor, _access, TestContext.Current.CancellationToken))
            .Where(row => row.Deleted).ToList();
        Assert.Equal(["agm", "no audience"], hidden.Select(row => row.RecordId));
        Assert.All(hidden, row => Assert.Empty(row.Payload));
    }

    private async Task<List<string>> ShownAsync(ClaimsPrincipal user, FeedChangeRecord[] rows) =>
        (await FeedVisibility.VisibleRowsAsync(new FeedDefinition("feed", FeedAudience.EachRow), [.. rows], user, _access, TestContext.Current.CancellationToken))
        .Where(row => !row.Deleted).Select(row => row.RecordId).ToList();

    private Task<bool> CanSeeAsync(FeedAudience audience, ClaimsPrincipal user) =>
        FeedVisibility.CanSeeFeedAsync(new FeedDefinition("feed", audience), user, _access, TestContext.Current.CancellationToken);

    private async Task<List<string>> VisibleAsync(FeedAudience audience, ClaimsPrincipal user, params FeedChangeRecord[] rows) =>
        (await FeedVisibility.VisibleRowsAsync(new FeedDefinition("feed", audience), [.. rows], user, _access, TestContext.Current.CancellationToken))
        .Select(row => row.RecordId).ToList();

    private static FeedChangeRecord Row(string recordId, string? scopeId) => new() { Feed = "feed", RecordId = recordId, ScopeId = scopeId, Version = 1, Payload = [1] };

    private static ClaimsPrincipal Person() => new(new ClaimsIdentity(
        [new Claim(Caller.UserIdClaim, Guid.CreateVersion7().ToString()), new Claim(Caller.SessionIdClaim, Guid.CreateVersion7().ToString())], "test"));

    private sealed class Permitted : IAccessEvaluator
    {
        private readonly HashSet<(Guid, string, string?)> _allowed = [];
        private readonly Dictionary<Guid, Guid> _roles = [];

        public void Allow(ClaimsPrincipal user, string node, string? scopeId) => _allowed.Add((Caller.UserId(user)!.Value, node, scopeId));

        public void Hold(ClaimsPrincipal user, Guid role) => _roles[Caller.UserId(user)!.Value] = role;

        public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Caller.UserId(user) is { } id && _allowed.Contains((id, node, scopeId)));

        public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(_roles.TryGetValue(userId, out Guid role)
                ? new GrantSet([new Grant(PermissionPattern.Parse(RoleMembership.NodeOf(role)), Scope.Global)])
                : GrantSet.Empty);
    }
}

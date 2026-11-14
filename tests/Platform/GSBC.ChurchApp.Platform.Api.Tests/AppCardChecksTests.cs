using System.Security.Claims;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Content;
using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Tests;

public sealed class AppCardChecksTests
{
    private const string SeeHiddenTeam = "ministries.hidden.see";

    private static readonly ClaimsPrincipal Writer = SignedIn();
    private static readonly ClaimsPrincipal TeamLeader = SignedIn();

    private readonly AppCardChecks _checks = new([new Ministries()], new PlaceAccess(new Grants(TeamLeader, SeeHiddenTeam)));

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ACardForSomethingThatExistsAndTheWriterCanSeeIsKept() =>
        Assert.Null(await ProblemAsync(Writer, $"Come along.\n\n::ministry{{id={Ministries.Open}}}"));

    [Fact]
    public async Task ACardForSomethingThatDoesntExistIsRefused() =>
        Assert.Contains("isn't in the app any more", await ProblemAsync(Writer, "::ministry{id=gone}"));

    [Fact]
    public async Task ACardForAHiddenMinistryIsRefusedAsIfItDidntExistToSomeoneWhoCantSeeIt()
    {
        string hidden = $"::ministry{{id={Ministries.Hidden}}}";

        Assert.Equal(await ProblemAsync(Writer, "::ministry{id=gone}"), await ProblemAsync(Writer, hidden));
        Assert.Null(await ProblemAsync(TeamLeader, hidden));
    }

    [Fact]
    public async Task AKindNoFeatureShowsIsRefused() =>
        Assert.Contains("a sermon", await ProblemAsync(Writer, "::sermon{id=8a1b}"));

    private Task<string?> ProblemAsync(ClaimsPrincipal writer, string markdown) =>
        _checks.ProblemAsync(writer, new RichText { Markdown = markdown }, Cancel);

    private static ClaimsPrincipal SignedIn() =>
        new(new ClaimsIdentity([new Claim(Caller.UserIdClaim, Guid.CreateVersion7().ToString())], "test"));

    private sealed class Ministries : IAppCardSource
    {
        public const string Open = "welcome-team";
        public const string Hidden = "safeguarding-team";

        public IReadOnlyList<AppCardKind> Kinds => [AppCardKind.Ministry];

        public Task<FoundPlace?> FindAsync(AppCard card, CancellationToken cancellationToken) => Task.FromResult(card.Id switch
        {
            Open => new FoundPlace(Open, "Welcome team", "Ministries", Audience.Everyone),
            Hidden => new FoundPlace(Hidden, "Safeguarding team", "Ministries", Audience.Holding(SeeHiddenTeam), Hidden),
            _ => null,
        });
    }

    private sealed class Grants(ClaimsPrincipal holder, string node) : IAccessEvaluator
    {
        public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string requested, string? scopeId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(user == holder && requested == node);

        public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult(GrantSet.Empty);
    }
}

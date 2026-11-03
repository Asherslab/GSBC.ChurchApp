namespace GSBC.ChurchApp.Platform.Permissions.Tests;

public sealed class GrantSetTests
{
    private static readonly Permission PostNews = new("ministries.news.post", ScopeKind.Ministry, "Post news");
    private static readonly Permission EditRoster = new("ministries.roster.edit", ScopeKind.Ministry, "Edit the roster");
    private static readonly Permission EditRoles = new("access.roles.edit", ScopeKind.Global, "Edit roles");

    private static Grant Global(string pattern) => new(PermissionPattern.Parse(pattern), Scope.Global);

    private static Grant AtMinistry(string pattern, string id) =>
        new(PermissionPattern.Parse(pattern), Scope.Of(ScopeKind.Ministry, id));

    [Fact]
    public void AScopedGrantOnlyCoversItsOwnObject()
    {
        GrantSet grants = new([AtMinistry("ministries.*", "42")]);

        Assert.True(grants.IsAllowed(PostNews, "42"));
        Assert.False(grants.IsAllowed(PostNews, "7"));
        Assert.False(grants.IsAllowed(EditRoles));
    }

    [Fact]
    public void AMoreSpecificPatternWinsEitherWay()
    {
        GrantSet allowExceptRoster = new([AtMinistry("ministries.*", "42"), AtMinistry("-ministries.roster.edit", "42")]);
        GrantSet denyExceptNews = new([Global("-ministries.*"), AtMinistry("ministries.news.post", "42")]);

        Assert.True(allowExceptRoster.IsAllowed(PostNews, "42"));
        Assert.False(allowExceptRoster.IsAllowed(EditRoster, "42"));
        Assert.True(denyExceptNews.IsAllowed(PostNews, "42"));
        Assert.False(denyExceptNews.IsAllowed(EditRoster, "42"));
    }

    [Fact]
    public void AGrantAtOneObjectCountsAsSomewhere()
    {
        Assert.True(new GrantSet([AtMinistry("ministries.news.post", "42")]).IsAllowedSomewhere(PostNews));
        Assert.False(new GrantSet([AtMinistry("ministries.news.post", "42"), AtMinistry("-ministries.news.post", "42")]).IsAllowedSomewhere(PostNews));
        Assert.False(new GrantSet([AtMinistry("ministries.roster.edit", "42")]).IsAllowedSomewhere(PostNews));
    }

    [Fact]
    public void DenyWinsATie() =>
        Assert.False(new GrantSet([Global("ministries.news.post"), AtMinistry("-ministries.news.post", "42")]).IsAllowed(PostNews, "42"));
}

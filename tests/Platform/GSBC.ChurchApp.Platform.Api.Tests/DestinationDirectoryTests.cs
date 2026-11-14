using System.Security.Claims;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Tests;

public sealed class DestinationDirectoryTests
{
    private static readonly Permission SeeLeadersFiles = new("ministries.files.view", ScopeKind.Global, "See leaders' files");
    private static readonly DestinationArea FilesArea = new("files", "Files", "file", 30);
    private static readonly DestinationKind File = new("test.file", FilesArea, "File", "file", "files", OpensOutside: true);
    private static readonly DestinationPage Previews = new("/previews", "Previews", "eye", DestinationAreas.Pages, "More → Previews", Audience.SignedIn);

    private static readonly ClaimsPrincipal Visitor = new();
    private static readonly ClaimsPrincipal Member = SignedIn();
    private static readonly ClaimsPrincipal Leader = SignedIn();

    private readonly DestinationDirectory _directory = new(
        new DestinationCatalog([ShellDestinations.All, new DestinationSet([Previews], [File])]),
        [new Files()],
        new PlaceAccess(new Grants(Leader, SeeLeadersFiles.Node)),
        new PermissionCatalog([new PermissionSet([SeeLeadersFiles])]));

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task APlaceSomeoneCantOpenIsNeitherListedNorDescribedToThem()
    {
        Assert.DoesNotContain("Leaders' roster.pdf", await TitlesAsync(Member, new DestinationSearch()));
        Destination withheld = await _directory.DescribeAsync(Member, File.PathOf(Files.Private), Cancel);
        Assert.Equal(DestinationState.NotForYou, withheld.State);
        Assert.Equal("", withheld.Title);
        Assert.Null(withheld.OpenUrl);

        Destination opened = await _directory.DescribeAsync(Leader, File.PathOf(Files.Private), Cancel);
        Assert.Equal(DestinationState.Available, opened.State);
        Assert.Equal("Only people allowed to see leaders' files", opened.WhoCanOpen);
        Assert.Equal("https://files.example/private", opened.OpenUrl);

        Assert.Equal(DestinationState.NotForYou, (await _directory.DescribeAsync(Visitor, Previews.Path, Cancel)).State);
    }

    [Fact]
    public async Task AnAnnouncementsPickerListsOnlyWhatEveryoneItReachesCanOpen()
    {
        IReadOnlyList<string> forEveryone = await TitlesAsync(Leader, new DestinationSearch { OpenableBy = Audience.Everyone.Text });
        Assert.Contains("Bulletin.pdf", forEveryone);
        Assert.Contains("What's new", forEveryone);
        Assert.DoesNotContain("Previews", forEveryone);
        Assert.DoesNotContain("Leaders' roster.pdf", forEveryone);

        Assert.Contains("Previews", await TitlesAsync(Leader, new DestinationSearch { OpenableBy = Audience.SignedIn.Text }));
    }

    [Fact]
    public async Task ATakenDownItemIsGoneAndAnUnknownPathIsUnknown()
    {
        Assert.Equal(DestinationState.Gone, (await _directory.DescribeAsync(Leader, File.PathOf("removed"), Cancel)).State);
        Assert.Equal(DestinationState.Unknown, (await _directory.DescribeAsync(Leader, "/sermons/1", Cancel)).State);
    }

    private async Task<IReadOnlyList<string>> TitlesAsync(ClaimsPrincipal user, DestinationSearch search) =>
        (await _directory.SearchAsync(user, search, Cancel)).Groups.SelectMany(group => group.Destinations).Select(destination => destination.Title).ToList();

    private static ClaimsPrincipal SignedIn() =>
        new(new ClaimsIdentity([new Claim(Caller.UserIdClaim, Guid.CreateVersion7().ToString())], "test"));

    private sealed class Files : IDestinationSource
    {
        public const string Public = "bulletin";
        public const string Private = "roster";

        private static readonly FoundPlace[] All =
        [
            new(Public, "Bulletin.pdf", "PDF", Audience.Everyone),
            new(Private, "Leaders' roster.pdf", "PDF", Audience.Holding(SeeLeadersFiles.Node)),
        ];

        public DestinationKind Kind => File;

        public Task<IReadOnlyList<FoundPlace>> SearchAsync(string text, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<FoundPlace>>(All.Where(place => place.Title.Contains(text, StringComparison.OrdinalIgnoreCase)).Take(take).ToList());

        public Task<FoundPlace?> FindAsync(string id, CancellationToken cancellationToken) =>
            Task.FromResult(All.FirstOrDefault(place => place.Id == id));

        public Task<string?> OpenUrlAsync(string id, CancellationToken cancellationToken) =>
            Task.FromResult<string?>($"https://files.example/{(id == Private ? "private" : "public")}");
    }

    private sealed class Grants(ClaimsPrincipal holder, string node) : IAccessEvaluator
    {
        public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string requested, string? scopeId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(user == holder && requested == node);

        public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult(GrantSet.Empty);
    }
}

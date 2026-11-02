using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

public sealed class PlacementEditorTests : IAsyncLifetime
{
    private static readonly Permission EditModules = new("ministries.modules.edit", ScopeKind.Ministry, "Arrange a ministry's modules");
    private static readonly SurfaceArea Modules = new("ministry.modules", "Modules", "", ScopeKind.Ministry, EditModules);
    private static readonly RichTextSetting About = new("about", "About");
    private static readonly Surface News = new("ministry.news", Modules, "News", "", []);
    private static readonly Surface Welcome = new("ministry.welcome", Modules, "Welcome", "", [About]);

    private static readonly SurfaceSet MinistrySurfaces = new([News, Welcome]);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private readonly AllowedAccess _access = new();
    private readonly Library _media = new();
    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        _services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _postgres.GetConnectionString())])
                .Build())
            .AddSingleton(TimeProvider.System)
            .AddSingleton(new SurfaceCatalog([HubSurfaces.All, MinistrySurfaces]))
            .AddSingleton(new PermissionCatalog([new PermissionSet([HubSurfaces.Edit, EditModules])]))
            .AddSingleton<IAccessEvaluator>(_access)
            .AddSingleton<IMediaReferences>(_media)
            .AddScoped<PlacementEditor>()
            .AddSyncData()
            .AddConfigurationData()
            .BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>().Database.MigrateAsync();
    }

    [Fact]
    public async Task OnlyHoldersOfTheAreasPermissionAtItsScopeCanArrangeIt()
    {
        ClaimsPrincipal leader = _access.Person((EditModules.Node, "42"));

        await SaveAsync(leader, Modules, "42", Hidden(News));

        Assert.Equal(StatusCode.PermissionDenied, (await Refused(() => SaveAsync(leader, Modules, "7", Hidden(News)))).StatusCode);
        Assert.Equal(StatusCode.PermissionDenied, (await Refused(() => SaveAsync(leader, HubSurfaces.Area, null, LinkTile("Bible")))).StatusCode);
    }

    [Fact]
    public async Task EveryValueIsCheckedAgainstItsDescriptorAndNothingInvalidIsStored()
    {
        ClaimsPrincipal staff = _access.Person((HubSurfaces.EditNode, null));
        Placement tile = LinkTile("Bible", link: "http://example.org");
        tile.Values.Add(new SettingValue { Key = "font", Value = "Comic Sans" });
        tile.Audience = "permission:nobody.declared.this";
        tile.StartsAt = new DateTime(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);
        tile.EndsAt = tile.StartsAt.Value.AddDays(-1);

        RpcException refused = await Refused(() => SaveAsync(staff, HubSurfaces.Area, null, tile));

        Assert.Equal(StatusCode.InvalidArgument, refused.StatusCode);
        Assert.Contains("https://", refused.Status.Detail);
        Assert.Contains("'font'", refused.Status.Detail);
        Assert.Contains("nobody.declared.this", refused.Status.Detail);
        Assert.Contains("end after it starts", refused.Status.Detail);
        Assert.Empty(await GetAsync(staff, HubSurfaces.Area, null));
    }

    [Fact]
    public async Task ASaveReachesPhonesThroughTheFeedAndARemovalDeletesThere()
    {
        ClaimsPrincipal staff = _access.Person((HubSurfaces.EditNode, null));
        Placement saved = (await SaveAsync(staff, HubSurfaces.Area, null, LinkTile("Bible"))).Single();

        await SaveAsync(staff, HubSurfaces.Area, null, removed: [new PlacementReference { Id = saved.Id, Revision = saved.Revision }]);

        List<FeedChangeRecord> feed = await FeedAsync();
        Assert.Equal([false, true], feed.Select(change => change.Deleted));
        Assert.All(feed, change => Assert.Equal(saved.Id, change.RecordId));
        Placement sent = ConfigurationContractsModel.Instance.Deserialize<Placement>(new MemoryStream(feed[0].Payload));
        Assert.Equal("Bible", sent.Values.Single(value => value.Key == HubSurfaces.LinkTile.Title.Key).Value);
        Assert.Equal(ConfigurationFeeds.GroupOf(HubSurfaces.Area.Key, null), feed[0].GroupKey);
    }

    [Fact]
    public async Task ASaveBasedOnAnOldRevisionOrASecondCopyOfASurfaceIsRefused()
    {
        ClaimsPrincipal staff = _access.Person((EditModules.Node, "42"));
        Placement first = (await SaveAsync(staff, Modules, "42", Hidden(News))).Single();
        Placement stale = Hidden(News);
        stale.Id = first.Id;
        stale.Revision = first.Revision;
        stale.Hidden = false;
        await SaveAsync(staff, Modules, "42", stale);

        Assert.Equal(StatusCode.Aborted, (await Refused(() => SaveAsync(staff, Modules, "42", stale))).StatusCode);
        Assert.Equal(StatusCode.Aborted, (await Refused(() => SaveAsync(staff, Modules, "42", Hidden(News)))).StatusCode);
    }

    [Fact]
    public async Task RollingBackASaveRestoresWhatWasThereAndIsAuditedItself()
    {
        ClaimsPrincipal staff = _access.Person((HubSurfaces.EditNode, null));
        Placement bible = (await SaveAsync(staff, HubSurfaces.Area, null, LinkTile("Bible"))).Single();
        Placement renamed = LinkTile("Read the Bible");
        renamed.Id = bible.Id;
        renamed.Revision = bible.Revision;
        await SaveAsync(staff, HubSurfaces.Area, null, renamed, LinkTile("Camp"));

        ChangeList history = await HistoryAsync(staff);
        await RollBackAsync(staff, history.Batches[0].Id);

        Placement restored = Assert.Single(await GetAsync(staff, HubSurfaces.Area, null));
        Assert.Equal(bible.Id, restored.Id);
        Assert.Equal("Bible", restored.Values.Single(value => value.Key == HubSurfaces.LinkTile.Title.Key).Value);
        ChangeBatch rollBack = (await HistoryAsync(staff)).Batches[0];
        Assert.Equal(history.Batches[0].Id, rollBack.RollsBack);
        Assert.Equal(2, rollBack.Changes.Count);
    }

    [Fact]
    public async Task ImagesInSettingsAreTheLibrarysAndForgedOrPrivateOnesAreRefused()
    {
        ClaimsPrincipal staff = _access.Person((HubSurfaces.EditNode, null));
        MediaRef photo = _media.Add(new MediaRef { Id = "photo", Key = $"{new string('a', 64)}.jpg", ContentType = "image/jpeg", Url = "https://media.example/a.jpg", Width = 800, Height = 600 });
        MediaRef notes = _media.Add(new MediaRef { Id = "notes", Key = $"{new string('b', 64)}.jpg", ContentType = "image/jpeg", IsPrivate = true });

        Placement tile = LinkTile("Camp");
        tile.Values.Add(Value(HubSurfaces.LinkTile.Image, new MediaRef { Id = photo.Id, Key = photo.Key, ContentType = "image/jpeg", Url = "https://attacker.example/x.jpg" }));
        Placement saved = Assert.Single(await SaveAsync(staff, HubSurfaces.Area, null, tile));

        Assert.True(HubSurfaces.LinkTile.Image.TryParse(saved.Values.Single(value => value.Key == "image").Value, out MediaRef? stored));
        Assert.Equal((photo.Url, 800), (stored!.Url, stored.Width));
        Assert.Equal([photo.Id], _media.UsedBy($"config.placement/{saved.Id}"));

        Placement forged = LinkTile("Forged");
        forged.Values.Add(Value(HubSurfaces.LinkTile.Image, new MediaRef { Id = "missing", ContentType = "image/jpeg" }));
        Placement privateImage = LinkTile("Private");
        privateImage.Values.Add(Value(HubSurfaces.LinkTile.Image, new MediaRef { Id = notes.Id, Key = notes.Key, ContentType = "image/jpeg" }));

        Assert.Equal(StatusCode.InvalidArgument, (await Refused(() => SaveAsync(staff, HubSurfaces.Area, null, forged))).StatusCode);
        Assert.Contains("public image", (await Refused(() => SaveAsync(staff, HubSurfaces.Area, null, privateImage))).Status.Detail);
        Assert.Single(await GetAsync(staff, HubSurfaces.Area, null));
    }

    [Fact]
    public async Task FormattedTextWithAnUnsafeLinkIsNeverStored()
    {
        ClaimsPrincipal leader = _access.Person((EditModules.Node, "42"));
        Placement welcome = new()
        {
            Surface = Welcome.Key,
            Values = [new SettingValue { Key = About.Key, Value = ContentJson.Write(new RichText { Markdown = "[Click me](javascript:alert(1))" }) }],
        };

        RpcException refused = await Refused(() => SaveAsync(leader, Modules, "42", welcome));

        Assert.Equal(StatusCode.InvalidArgument, refused.StatusCode);
        Assert.Empty(await GetAsync(leader, Modules, "42"));
    }

    private static SettingValue Value(MediaSetting setting, MediaRef media) => new() { Key = setting.Key, Value = setting.Format(media) };

    private static Placement LinkTile(string title, string link = "https://www.biblegateway.com") => new()
    {
        Surface = HubSurfaces.LinkTile.Surface.Key,
        Values =
        [
            new SettingValue { Key = HubSurfaces.LinkTile.Title.Key, Value = title },
            new SettingValue { Key = HubSurfaces.LinkTile.Link.Key, Value = link },
        ],
    };

    private static Placement Hidden(Surface surface) => new() { Surface = surface.Key, Hidden = true };

    private Task<List<Placement>> SaveAsync(ClaimsPrincipal caller, SurfaceArea area, string? scopeId, params Placement[] placements) =>
        SaveAsync(caller, area, scopeId, removed: [], placements);

    private Task<List<Placement>> SaveAsync(ClaimsPrincipal caller, SurfaceArea area, string? scopeId, List<PlacementReference> removed, params Placement[] placements) =>
        WithEditorAsync(async editor => (await editor.SaveAsync(caller,
            new SaveRequest { Area = area.Key, ScopeId = scopeId, Placements = [.. placements], Removed = removed },
            TestContext.Current.CancellationToken)).Placements);

    private Task<List<Placement>> GetAsync(ClaimsPrincipal caller, SurfaceArea area, string? scopeId) =>
        WithEditorAsync(async editor => (await editor.GetAreaAsync(caller, new AreaQuery { Area = area.Key, ScopeId = scopeId },
            TestContext.Current.CancellationToken)).Placements);

    private Task<ChangeList> HistoryAsync(ClaimsPrincipal caller) =>
        WithEditorAsync(editor => editor.ListChangesAsync(caller, new AreaQuery { Area = HubSurfaces.Area.Key }, TestContext.Current.CancellationToken));

    private Task<AreaPlacements> RollBackAsync(ClaimsPrincipal caller, string batchId) =>
        WithEditorAsync(editor => editor.RollBackAsync(caller, new RollBackRequest { BatchId = batchId }, TestContext.Current.CancellationToken));

    private async Task<List<FeedChangeRecord>> FeedAsync()
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SyncDbContext>().FeedChanges.AsNoTracking()
            .Where(change => change.Feed == ConfigurationFeeds.Placements)
            .OrderBy(change => change.Cursor)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private async Task<T> WithEditorAsync<T>(Func<PlacementEditor, Task<T>> action)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<PlacementEditor>());
    }

    private static Task<RpcException> Refused(Func<Task> action) => Assert.ThrowsAsync<RpcException>(action);

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private sealed class Library : IMediaReferences
    {
        private readonly Dictionary<string, MediaRef> _items = [];
        private readonly Dictionary<string, List<string>> _uses = [];

        public MediaRef Add(MediaRef media) => _items[media.Id] = media;

        public List<string> UsedBy(string owner) => _uses.GetValueOrDefault(owner, []);

        public Task<IReadOnlyList<MediaRef>> VerifyAsync(IReadOnlyCollection<MediaRef> claimed, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MediaRef>>(claimed
                .Select(media => _items.TryGetValue(media.Id, out MediaRef? known) && (media.Key.Length == 0 || media.Key == known.Key)
                    ? known
                    : throw new RpcException(new Status(StatusCode.InvalidArgument, "Not in the library.")))
                .ToList());

        public Task UseAsync(string owner, IReadOnlyCollection<MediaRef> verified, CancellationToken cancellationToken)
        {
            _uses[owner] = verified.Select(media => media.Id).ToList();
            return Task.CompletedTask;
        }
    }

    private sealed class AllowedAccess : IAccessEvaluator
    {
        private readonly Dictionary<Guid, HashSet<(string Node, string? ScopeId)>> _allowed = [];

        public ClaimsPrincipal Person(params (string Node, string? ScopeId)[] allowed)
        {
            Guid id = Guid.CreateVersion7();
            _allowed[id] = [.. allowed];
            return new ClaimsPrincipal(new ClaimsIdentity([new Claim(Caller.UserIdClaim, id.ToString())], "test"));
        }

        public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Caller.UserId(user) is { } id && _allowed[id].Contains((node, scopeId)));

        public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult(GrantSet.Empty);
    }
}

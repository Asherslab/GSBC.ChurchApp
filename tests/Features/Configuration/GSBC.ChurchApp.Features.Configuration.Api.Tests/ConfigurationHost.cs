using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;
using Testcontainers.PostgreSql;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

public abstract class ConfigurationHost : IAsyncLifetime
{
    protected static readonly Permission EditModules = new("ministries.modules.edit", ScopeKind.Ministry, "Arrange a ministry's modules");
    protected static readonly Permission PublishModules = new("ministries.modules.publish", ScopeKind.Ministry, "Publish a ministry's modules");
    protected static readonly SurfaceArea Modules = new("ministry.modules", "Modules", "", ScopeKind.Ministry, EditModules, PublishModules);
    protected static readonly RichTextSetting About = new("about", "About");
    protected static readonly Surface News = new("ministry.news", Modules, "News", "", []);
    protected static readonly Surface Welcome = new("ministry.welcome", Modules, "Welcome", "", [About]);
    protected static readonly (string, string?) ArrangesHub = (HubSurfaces.EditNode, null);
    protected static readonly (string, string?) PublishesHub = (HubSurfaces.PublishNode, null);
    protected static readonly (string, string?) SeesEveryDraft = (ConfigurationPermissions.PreviewDraft.Node, null);

    private static readonly SurfaceSet MinistrySurfaces = new([News, Welcome]);
    private static readonly PermissionCatalog Permissions = new([new PermissionSet(
        [HubSurfaces.Edit, HubSurfaces.Publish, AppearanceSurfaces.Edit, AppearanceSurfaces.Publish, EditModules, PublishModules, ConfigurationPermissions.PreviewDraft])]);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private ServiceProvider _services = null!;

    protected FakeAccess Access { get; } = new(Permissions);
    protected Library Media { get; } = new();
    protected RaisedNotifications Raised { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        _services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _postgres.GetConnectionString())])
                .Build())
            .AddSingleton(TimeProvider.System)
            .AddSingleton(new SurfaceCatalog([HubSurfaces.All, AppearanceSurfaces.All, MinistrySurfaces, WaitingTiles.Surfaces]))
            .AddSingleton(Permissions)
            .AddSingleton<IAccessEvaluator>(Access)
            .AddSingleton<IRoleAssignments>(Access)
            .AddSingleton<IMediaReferences>(Media)
            .AddSingleton<INotifications>(Raised)
            .AddSingleton(new FeedRegistry([
                new FeedDefinition(ConfigurationFeeds.Placements, FeedAudience.Public),
                new FeedDefinition(ConfigurationFeeds.Drafts, FeedAudience.Holding(ConfigurationPermissions.PreviewDraft))]))
            .AddSingleton<FeedChangeBroadcaster>()
            .AddScoped<SyncService>()
            .AddSingleton<ISignedInSessions, AlwaysSignedIn>()
            .AddScoped<PlacementEditor>()
            .AddScoped<DraftEditor>()
            .AddScoped<IMediaHolder, PlacementMediaHolder>()
            .AddSyncData()
            .AddConfigurationData()
            .BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>().Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected static CancellationToken Cancel => TestContext.Current.CancellationToken;

    protected async Task<T> Use<TService, T>(Func<TService, Task<T>> action) where TService : notnull
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    private protected Task<T> Drafts<T>(Func<DraftEditor, Task<T>> action) => Use(action);

    private protected Task<T> Placements<T>(Func<PlacementEditor, Task<T>> action) => Use(action);

    protected Task<Draft> NewDraftAsync(ClaimsPrincipal caller, string name = "Christmas 2026") =>
        Drafts(editor => editor.CreateAsync(caller, new NewDraftRequest { Name = name }, Cancel));

    protected Task<Draft> SaveAsync(ClaimsPrincipal caller, Draft draft, params PatchMessage[] patches) =>
        Drafts(editor => editor.SaveAsync(caller, new SaveDraftRequest { DraftId = draft.Id, Set = [.. patches] }, Cancel));

    protected Task<PublishReply> PublishAsync(ClaimsPrincipal caller, Draft draft) =>
        Drafts(editor => editor.PublishAsync(caller, new DraftQuery { DraftId = draft.Id }, Cancel));

    protected async Task<List<Placement>> PublishNowAsync(ClaimsPrincipal caller, SurfaceArea area, string? scopeId, params PatchMessage[] patches)
    {
        Draft draft = await NewDraftAsync(caller, "A quick fix");
        await SaveAsync(caller, draft, patches);
        await PublishAsync(caller, draft);
        return await LiveAsync(caller, area, scopeId);
    }

    protected Task<List<Placement>> LiveAsync(ClaimsPrincipal caller, SurfaceArea area, string? scopeId = null) =>
        Placements(async editor => (await editor.GetAreaAsync(caller, new AreaQuery { Area = area.Key, ScopeId = scopeId }, Cancel)).Placements);

    protected async Task<List<FeedChangeRecord>> FeedAsync(string feed)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SyncDbContext>().FeedChanges.AsNoTracking()
            .Where(change => change.Feed == feed)
            .OrderBy(change => change.Cursor)
            .ToListAsync(Cancel);
    }

    protected Task<PullReply> PullAsync(ClaimsPrincipal caller, string feed, long cursor) =>
        Use<SyncService, PullReply>(async sync => await sync.PullAsync(
            new PullRequest { Cursors = [new FeedCursor { Feed = feed, Cursor = cursor }] }, new CallContext(caller, new PersonCallContext(caller))));

    protected static PatchMessage Add(Surface surface, SurfaceArea area, string? scopeId = null, params (string Key, string Value)[] values) => new()
    {
        PlacementId = Guid.CreateVersion7().ToString(),
        Area = area.Key,
        ScopeId = scopeId,
        Surface = surface.Key,
        Kind = (int)PatchKind.Add,
        Values = values.Select(value => new ValueChange { Key = value.Key, Value = value.Value }).ToList(),
    };

    protected static PatchMessage LinkTile(string title, string link = "https://www.biblegateway.com") =>
        Add(HubSurfaces.LinkTile.Surface, HubSurfaces.Area, null, (HubSurfaces.LinkTile.Title.Key, title), (HubSurfaces.LinkTile.Link.Key, link));

    protected static PatchMessage Change(Placement live, params (string Key, string? Value)[] values) => new()
    {
        PlacementId = live.Id,
        Area = live.Area,
        ScopeId = live.ScopeId,
        Surface = live.Surface,
        Kind = (int)PatchKind.Change,
        Values = values.Select(value => new ValueChange { Key = value.Key, Value = value.Value }).ToList(),
    };

    protected static PatchMessage Remove(Placement live) => new()
    {
        PlacementId = live.Id, Area = live.Area, ScopeId = live.ScopeId, Surface = live.Surface, Kind = (int)PatchKind.Remove,
    };

    protected static string ValueOf(Placement placement, string key) => placement.Values.Single(value => value.Key == key).Value;

    protected static Task<RpcException> Refused(Func<Task> action) => Assert.ThrowsAsync<RpcException>(action);

    private sealed class PersonCallContext(ClaimsPrincipal person) : ServerCallContext
    {
        private readonly Dictionary<object, object> _state = new() { ["__HttpContext"] = new DefaultHttpContext { User = person } };

        protected override IDictionary<object, object> UserStateCore => _state;
        protected override string MethodCore => "";
        protected override string HostCore => "";
        protected override string PeerCore => "";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => [];
        protected override CancellationToken CancellationTokenCore => Cancel;
        protected override Metadata ResponseTrailersCore => [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new(null, []);

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }

    protected sealed class Library : IMediaReferences
    {
        private readonly Dictionary<string, MediaRef> _items = [];
        private readonly Dictionary<string, List<string>> _uses = [];
        private readonly HashSet<string> _formerKeys = [];

        public MediaRef Add(MediaRef media) => _items[media.Id] = media;

        public MediaRef Replace(MediaRef media)
        {
            _formerKeys.Add(_items[media.Id].Key);
            return Add(media);
        }

        public List<string> UsedBy(string owner) => _uses.GetValueOrDefault(owner, []);

        public Task<IReadOnlyList<MediaRef>> VerifyAsync(IReadOnlyCollection<MediaRef> claimed, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MediaRef>>(claimed
                .Select(media => _items.TryGetValue(media.Id, out MediaRef? known) && (media.Key.Length == 0 || media.Key == known.Key || _formerKeys.Contains(media.Key))
                    ? known.IsPrivate ? throw new RpcException(new Status(StatusCode.InvalidArgument, "Settings need a public image.")) : known
                    : throw new RpcException(new Status(StatusCode.InvalidArgument, "Not in the library.")))
                .ToList());

        public Task UseAsync(string owner, IReadOnlyCollection<MediaRef> verified, CancellationToken cancellationToken)
        {
            _uses[owner] = verified.Select(media => media.Id).ToList();
            return Task.CompletedTask;
        }
    }

    protected sealed class RaisedNotifications : INotifications
    {
        public List<Notification> All { get; } = [];

        public Task RaiseAsync(Notification notification, CancellationToken cancellationToken)
        {
            All.Add(notification);
            return Task.CompletedTask;
        }

        public List<string> Settled { get; } = [];

        public Task SettleAsync(string subject, CancellationToken cancellationToken)
        {
            Settled.Add(subject);
            return Task.CompletedTask;
        }
    }

    protected sealed class FakeAccess(PermissionCatalog catalog) : IAccessEvaluator, IRoleAssignments
    {
        private readonly Dictionary<Guid, List<Grant>> _grants = [];

        public ClaimsPrincipal Person(params (string Node, string? ScopeId)[] allowed)
        {
            Guid id = Guid.CreateVersion7();
            _grants[id] = allowed.Select(entry => GrantOf(entry.Node, entry.ScopeId)).ToList();
            return new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(Caller.UserIdClaim, id.ToString()), new Claim(Caller.SessionIdClaim, Guid.CreateVersion7().ToString())], "test"));
        }

        public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Caller.UserId(user) is { } id && catalog.ByNode.TryGetValue(node, out Permission? permission)
                && new GrantSet(_grants[id]).IsAllowed(permission, scopeId));

        public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new GrantSet([.. _grants.GetValueOrDefault(userId, [])]));

        public ValueTask GiveAsFeatureAsync(Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken)
        {
            Grant reviewer = GrantOf(ConfigurationPermissions.PreviewDraft.Node, objectId);
            List<Grant> held = _grants.TryGetValue(userId, out List<Grant>? existing) ? existing : _grants[userId] = [];
            if (!held.Any(grant => Same(grant, reviewer)))
                held.Add(reviewer);
            return ValueTask.CompletedTask;
        }

        public ValueTask TakeAwayAsFeatureAsync(Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken)
        {
            Grant reviewer = GrantOf(ConfigurationPermissions.PreviewDraft.Node, objectId);
            _grants.GetValueOrDefault(userId)?.RemoveAll(grant => Same(grant, reviewer));
            return ValueTask.CompletedTask;
        }

        public ValueTask<IReadOnlyList<RoleHolder>> HoldersAsync(string roleKey, string? objectId, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<RoleHolder>>(_grants
                .Where(pair => pair.Value.Any(grant => Same(grant, GrantOf(ConfigurationPermissions.PreviewDraft.Node, objectId))))
                .Select(pair => new RoleHolder(pair.Key))
                .ToList());

        public ValueTask<AssignmentOutcome> GiveAsync(Guid actorId, Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask<AssignmentOutcome> TakeAwayAsync(Guid actorId, Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask<bool> MayGiveAsync(Guid actorId, string roleKey, string? objectId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask ScopesChangedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public static Guid IdOf(ClaimsPrincipal person) => Caller.UserId(person)!.Value;

        private static bool Same(Grant left, Grant right) => left.Pattern.Text == right.Pattern.Text && left.Scope == right.Scope;

        private Grant GrantOf(string node, string? scopeId)
        {
            Permission permission = catalog.ByNode[node];
            return new Grant(PermissionPattern.Parse(node), scopeId is null ? Scope.Global : Scope.Of(permission.ScopeKind, scopeId));
        }
    }

    private sealed class AlwaysSignedIn : ISignedInSessions
    {
        public ValueTask<bool> IsCurrentAsync(string sessionId, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }
}

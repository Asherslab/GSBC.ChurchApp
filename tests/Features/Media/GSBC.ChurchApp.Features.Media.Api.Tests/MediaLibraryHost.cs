using System.Net.Http.Headers;
using System.Security.Claims;
using Amazon.S3;
using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Platform.Storage;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

public abstract class MediaLibraryHost : IAsyncLifetime
{
    private protected const string Ministry = "42";

    private protected static readonly Permission ReadFiles = new("ministries.files.read", ScopeKind.Ministry, "Read a ministry's files");
    private protected static readonly MediaSystemArea Chats = new("chats", "Chats", "Made by the app: files people share in group chats", ReadFiles);
    private protected static readonly Permission PostFiles = new("ministries.posts.write", ScopeKind.Ministry, "Add files to a ministry");
    private protected static readonly MediaSystemArea MinistryFiles =
        new("ministry-files", "Ministry files", "Made by the app: documents leaders add to their ministries", ReadFiles, Uploaders: PostFiles);

    private protected TestBuckets Buckets { get; private set; } = null!;
    private TestDatabase _database = null!;
    private readonly TestClock _clock = new();
    private protected GrantedEvaluator Access { get; } = new();
    private protected TileHolder Tiles { get; } = new();
    private protected ChatMembers Members { get; } = new();
    private protected HttpClient Http { get; } = new();
    private protected ServiceProvider Services { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        Buckets = await TestBuckets.CreateAsync();
        string serviceUrl = Buckets.ServiceUrl;

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{Database.ConnectionStringName}"] = _database.ConnectionString,
            ["Storage:Blobs:ServiceUrl"] = serviceUrl,
            ["Storage:Blobs:AccessKeyId"] = Buckets.AccessKey,
            ["Storage:Blobs:SecretAccessKey"] = Buckets.SecretKey,
            ["Storage:Blobs:PublicBucket"] = Buckets.Public,
            ["Storage:Blobs:PrivateBucket"] = Buckets.Private,
            ["Storage:Blobs:PublicBaseUrl"] = $"{serviceUrl}/{Buckets.Public}",
        }).Build();

        Services = new ServiceCollection()
            .AddSingleton(configuration)
            .AddLogging()
            .AddSingleton<TimeProvider>(_clock)
            .AddSingleton(new PermissionCatalog([new PermissionSet([.. MediaPermissions.All, ReadFiles, PostFiles])]))
            .AddSingleton<IAccessEvaluator>(Access)
            .AddBlobStore(configuration)
            .AddMediaData()
            .AddSyncData()
            .AddScoped<MediaLibrary>()
            .AddScoped<MediaSizer>()
            .AddScoped<MediaHolders>()
            .AddSingleton<IMediaHolder>(Tiles)
            .AddSingleton(Chats)
            .AddSingleton(MinistryFiles)
            .AddSingleton<IMediaAreaAccess>(Members)
            .AddScoped<MediaFolders>()
            .AddScoped<IMediaReferences, MediaReferences>()
            .BuildServiceProvider();

        IAmazonS3 client = Services.GetRequiredService<IAmazonS3>();
        await client.PutBucketAsync(Buckets.Public);
        await client.PutBucketAsync(Buckets.Private);

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<MediaDbContext>().Database.MigrateAsync();
    }

    private protected async Task<MediaRef> UploadAsync(ClaimsPrincipal user, byte[] bytes, string contentType, MediaAudience? privateTo = null, string fileName = "file") =>
        (await UploadAgainAsync(user, bytes, contentType, privateTo, fileName)).Item!.Media!;

    private protected async Task<UploadedMedia> UploadAgainAsync(
        ClaimsPrincipal user, byte[] bytes, string contentType, MediaAudience? privateTo = null, string fileName = "file", bool addAgain = false)
    {
        Access.Grant(user, MediaPermissions.UploadNode, null);
        return await SendAsync(user, bytes, new BeginUploadRequest { FileName = fileName, ContentType = contentType, Bytes = bytes.Length, PrivateTo = privateTo }, addAgain);
    }

    private protected async Task<UploadedMedia> SendAsync(ClaimsPrincipal user, byte[] bytes, BeginUploadRequest request, bool addAgain = false)
    {
        UploadTicket ticket = await LibraryCallAsync(library => library.BeginUploadAsync(user, null, request, Cancellation));

        using ByteArrayContent body = new(bytes);
        body.Headers.ContentType = new MediaTypeHeaderValue(ticket.ContentType);
        using HttpResponseMessage put = await Http.PutAsync(ticket.Url, body, Cancellation);
        put.EnsureSuccessStatusCode();

        return await LibraryCallAsync(library => library.CompleteUploadAsync(user, null, new CompleteUploadRequest { UploadId = ticket.UploadId, AddAgain = addAgain }, Cancellation));
    }

    private protected async Task<T> FoldersCallAsync<T>(Func<MediaFolders, Task<T>> call)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<MediaFolders>());
    }

    private protected async Task<T> LibraryCallAsync<T>(Func<MediaLibrary, Task<T>> call)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<MediaLibrary>());
    }

    private protected async Task<T> ReferencesCallAsync<T>(Func<IMediaReferences, Task<T>> call)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<IMediaReferences>());
    }

    private protected static async Task<bool> Done(Task task)
    {
        await task;
        return true;
    }

    private protected string PublicUrlBase => $"{Buckets.ServiceUrl}/{Buckets.Public}";

    private protected static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private protected static ClaimsPrincipal Person() =>
        new(new ClaimsIdentity([new Claim(Caller.UserIdClaim, Guid.CreateVersion7().ToString())], "test"));

    private protected static byte[] Jpeg(int seed) => SamplePictures.Jpeg(seed);

    private protected static readonly Guid ArtworkFolder = Guid.CreateVersion7();
    private protected static readonly Guid LargeArtwork = Guid.CreateVersion7();
    private protected static readonly Guid SmallArtwork = Guid.CreateVersion7();
    private protected static readonly Guid TwiceLarge = Guid.CreateVersion7();
    private protected static readonly Guid TwiceLargeAgain = Guid.CreateVersion7();
    private protected static readonly Guid TwiceSmall = Guid.CreateVersion7();

    private protected static byte[] Picture(int width, int height) => SamplePictures.Picture(width, height);

    public async ValueTask DisposeAsync()
    {
        Http.Dispose();
        await Services.DisposeAsync();
        await _database.DisposeAsync();
    }

    private protected sealed class ChatMembers : IMediaAreaAccess
    {
        private readonly HashSet<(string User, string Chat)> _members = [];

        public string Area => Chats.Key;

        public void Add(ClaimsPrincipal user, string chat) => _members.Add((user.FindFirstValue(Caller.UserIdClaim)!, chat));

        public Task<bool> MayUploadAsync(ClaimsPrincipal user, string scopeId, CancellationToken cancellationToken) => Task.FromResult(IsMember(user, scopeId));

        public Task<bool> MayReadAsync(ClaimsPrincipal user, string scopeId, CancellationToken cancellationToken) => Task.FromResult(IsMember(user, scopeId));

        private bool IsMember(ClaimsPrincipal user, string chat) => _members.Contains((user.FindFirstValue(Caller.UserIdClaim)!, chat));
    }

    private protected sealed class TileHolder : IMediaHolder
    {
        public const string Tile = "tile/1";

        public List<string> Refreshed { get; } = [];

        public bool Holds(string owner) => owner.StartsWith("tile/", StringComparison.Ordinal);

        public Task RefreshAsync(string owner, CancellationToken cancellationToken)
        {
            Refreshed.Add(owner);
            return Task.CompletedTask;
        }
    }
}

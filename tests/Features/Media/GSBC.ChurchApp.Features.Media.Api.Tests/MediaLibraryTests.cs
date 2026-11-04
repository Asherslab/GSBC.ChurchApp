using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Amazon.S3;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

public sealed class MediaLibraryTests : IAsyncLifetime
{
    private const int S3Port = 8333;
    private const string AccessKey = "test-access-key";
    private const string SecretKey = "test-secret-key";
    private const string PublicBucket = "media";
    private const string PrivateBucket = "private";
    private const string Ministry = "42";

    private static readonly Permission ReadFiles = new("ministries.files.read", ScopeKind.Ministry, "Read a ministry's files");
    private static readonly MediaSystemArea Chats = new("chats", "Chats", "Made by the app: files people share in group chats", ReadFiles);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private readonly IContainer _s3 = new ContainerBuilder("chrislusf/seaweedfs:4.50")
        .WithCommand("mini", "-dir=/data", "-s3.config=/etc/gsbc/s3.json")
        .WithResourceMapping(Encoding.UTF8.GetBytes($$"""
            {"identities":[
              {"name":"api","credentials":[{"accessKey":"{{AccessKey}}","secretKey":"{{SecretKey}}"}],"actions":["Admin","Read","Write","List"]},
              {"name":"anonymous","actions":["Read:{{PublicBucket}}"]}
            ]}
            """), "/etc/gsbc/s3.json")
        .WithPortBinding(S3Port, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request
            .ForPort(S3Port).ForPath("/").ForStatusCode(HttpStatusCode.OK).ForStatusCode(HttpStatusCode.Forbidden)))
        .Build();

    private readonly GrantedEvaluator _access = new();
    private readonly HttpClient _http = new();
    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _s3.StartAsync());
        string serviceUrl = $"http://{_s3.Hostname}:{_s3.GetMappedPublicPort(S3Port)}";

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{Database.ConnectionStringName}"] = _postgres.GetConnectionString(),
            ["Storage:Blobs:ServiceUrl"] = serviceUrl,
            ["Storage:Blobs:AccessKeyId"] = AccessKey,
            ["Storage:Blobs:SecretAccessKey"] = SecretKey,
            ["Storage:Blobs:PublicBucket"] = PublicBucket,
            ["Storage:Blobs:PrivateBucket"] = PrivateBucket,
            ["Storage:Blobs:PublicBaseUrl"] = $"{serviceUrl}/{PublicBucket}",
        }).Build();

        _services = new ServiceCollection()
            .AddSingleton(configuration)
            .AddLogging()
            .AddSingleton(TimeProvider.System)
            .AddSingleton(new PermissionCatalog([new PermissionSet([.. MediaPermissions.All, ReadFiles])]))
            .AddSingleton<IAccessEvaluator>(_access)
            .AddBlobStore(configuration)
            .AddMediaData()
            .AddSyncData()
            .AddScoped<MediaLibrary>()
            .AddSingleton(Chats)
            .AddScoped<MediaFolders>()
            .AddScoped<IMediaReferences, MediaReferences>()
            .BuildServiceProvider();

        IAmazonS3 s3 = _services.GetRequiredService<IAmazonS3>();
        await s3.PutBucketAsync(PublicBucket);
        await s3.PutBucketAsync(PrivateBucket);

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<MediaDbContext>().Database.MigrateAsync();
    }

    [Fact]
    public async Task AFilesNameIsItsContentSoNewBytesGetANewNameAndOldNamesNeverChange()
    {
        ClaimsPrincipal staff = Person();
        byte[] photo = Jpeg(seed: 1);
        byte[] edited = Jpeg(seed: 2);

        MediaRef first = await UploadAsync(staff, photo, "image/jpeg");
        MediaRef again = await UploadAsync(staff, photo, "image/jpeg");
        MediaRef changed = await UploadAsync(staff, edited, "image/jpeg");

        Assert.Equal($"{Convert.ToHexStringLower(SHA256.HashData(photo))}.jpg", first.Key);
        Assert.Equal(first.Key, again.Key);
        Assert.NotEqual(first.Key, changed.Key);

        using HttpResponseMessage served = await _http.GetAsync(first.Url, Cancellation);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal(photo, await served.Content.ReadAsByteArrayAsync(Cancellation));
        Assert.Equal("image/jpeg", served.Content.Headers.ContentType?.MediaType);
        Assert.Equal(ContentAddress.ImmutableCacheControl, served.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task BytesThatAreNotTheTypeTheyClaimAreRefusedAndNotKept()
    {
        ClaimsPrincipal staff = Person();

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() =>
            UploadAsync(staff, Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>"), "image/png"));

        Assert.Equal(StatusCode.InvalidArgument, refused.StatusCode);
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        MediaDbContext db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        Assert.Empty(await db.Items.ToListAsync(Cancellation));
        Assert.Empty(await db.PendingUploads.ToListAsync(Cancellation));
    }

    [Fact]
    public async Task PrivateMediaIsOnlyLinkedWhileTheCallerStillHoldsItsAudience()
    {
        ClaimsPrincipal leader = Person();
        ClaimsPrincipal member = Person();
        ClaimsPrincipal outsider = Person();
        _access.Grant(leader, ReadFiles.Node, Ministry);
        _access.Grant(member, ReadFiles.Node, Ministry);
        byte[] notes = Encoding.ASCII.GetBytes("%PDF-1.7 the group's notes");

        MediaRef file = await UploadAsync(leader, notes, "application/pdf", new MediaAudience { Permission = ReadFiles.Node, ScopeId = Ministry });

        Assert.True(file.IsPrivate);
        Assert.Null(file.Url);
        using (HttpResponseMessage anonymous = await _http.GetAsync($"{PublicUrlBase}/{file.Key}", Cancellation))
            Assert.False(anonymous.IsSuccessStatusCode);

        MediaLink link = await LibraryCallAsync(library => library.GetLinkAsync(member, file.Id, Cancellation));
        Assert.Equal(notes, await _http.GetByteArrayAsync(link.Url, Cancellation));

        _access.Revoke(member, ReadFiles.Node, Ministry);
        RpcException removed = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => library.GetLinkAsync(member, file.Id, Cancellation)));
        RpcException never = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => library.GetLinkAsync(outsider, file.Id, Cancellation)));
        Assert.Equal(StatusCode.PermissionDenied, removed.StatusCode);
        Assert.Equal(StatusCode.PermissionDenied, never.StatusCode);
    }

    [Fact]
    public async Task NobodyCanMakeAFilePrivateToPeopleTheyAreNotAmong()
    {
        ClaimsPrincipal staff = Person();

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => UploadAsync(staff, Jpeg(seed: 3), "image/jpeg",
            new MediaAudience { Permission = ReadFiles.Node, ScopeId = Ministry }));

        Assert.Equal(StatusCode.PermissionDenied, refused.StatusCode);
    }

    [Fact]
    public async Task OnlyPeopleTrustedWithFontLicencesCanUploadAFontFile()
    {
        ClaimsPrincipal staff = Person();
        byte[] font = [.. "wOF2"u8, .. new byte[64]];

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => UploadAsync(staff, font, "font/woff2"));
        _access.Grant(staff, MediaPermissions.UploadFontsNode, null);
        MediaRef uploaded = await UploadAsync(staff, font, "font/woff2");

        Assert.Equal(StatusCode.PermissionDenied, refused.StatusCode);
        Assert.True(uploaded.IsFont);
    }

    [Fact]
    public async Task ASavedMediaRefIsTheLibrarysNeverTheClients()
    {
        MediaRef photo = await UploadAsync(Person(), Jpeg(seed: 4), "image/jpeg");
        MediaRef forgedDetails = new()
        {
            Id = photo.Id,
            Key = photo.Key,
            ContentType = "text/html",
            Bytes = 1,
            Url = "https://attacker.example/x.jpg",
            IsPrivate = true,
            AltText = "The hall set up for lunch",
        };

        MediaRef saved = Assert.Single(await ReferencesCallAsync(references => references.VerifyAsync([forgedDetails], Cancellation)));

        Assert.Equal((photo.ContentType, photo.Bytes, photo.Url, photo.IsPrivate), (saved.ContentType, saved.Bytes, saved.Url, saved.IsPrivate));
        Assert.Equal("The hall set up for lunch", saved.AltText);

        MediaRef otherKey = new() { Id = photo.Id, Key = $"{new string('0', 64)}.jpg" };
        MediaRef unknown = new() { Id = Guid.CreateVersion7().ToString(), Key = photo.Key };
        foreach (MediaRef forged in new[] { otherKey, unknown })
        {
            RpcException refused = await Assert.ThrowsAsync<RpcException>(() =>
                ReferencesCallAsync(references => references.VerifyAsync([forged], Cancellation)));
            Assert.Equal(StatusCode.InvalidArgument, refused.StatusCode);
        }
    }

    [Fact]
    public async Task ImageDimensionsComeFromTheBytesTurnedTheWayThePhotoIsShown()
    {
        ClaimsPrincipal staff = Person();

        MediaRef png = await UploadAsync(staff, Png(width: 640, height: 480), "image/png");
        MediaRef sidewaysPhoto = await UploadAsync(staff, JpegWithOrientation(width: 4000, height: 3000, orientation: 6), "image/jpeg");

        Assert.Equal((640, 480), (png.Width, png.Height));
        Assert.Equal((3000, 4000), (sidewaysPhoto.Width, sidewaysPhoto.Height));
    }

    [Fact]
    public async Task MediaInUseCanNotBeDeletedAndBytesStayWhileAnotherCopyIsListed()
    {
        ClaimsPrincipal staff = Person();
        byte[] bytes = Jpeg(seed: 5);
        MediaRef used = await UploadAsync(staff, bytes, "image/jpeg");
        MediaRef sameBytes = await UploadAsync(staff, bytes, "image/jpeg");
        await ReferencesCallAsync(references => Done(references.UseAsync("news.post/3", [used], Cancellation)));

        RpcException inUse = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => Done(library.DeleteAsync(used.Id, Cancellation))));
        Assert.Equal(StatusCode.FailedPrecondition, inUse.StatusCode);

        await ReferencesCallAsync(references => Done(references.UseAsync("news.post/3", [], Cancellation)));
        await LibraryCallAsync(library => Done(library.DeleteAsync(used.Id, Cancellation)));
        using (HttpResponseMessage stillListed = await _http.GetAsync(sameBytes.Url, Cancellation))
            Assert.Equal(HttpStatusCode.OK, stillListed.StatusCode);

        await LibraryCallAsync(library => Done(library.DeleteAsync(sameBytes.Id, Cancellation)));
        using (HttpResponseMessage gone = await _http.GetAsync(sameBytes.Url, Cancellation))
            Assert.False(gone.IsSuccessStatusCode);
    }

    [Fact]
    public async Task MovingFilesAndFoldersAroundNeverChangesAFilesIdentityOrWhereItIsUsed()
    {
        ClaimsPrincipal staff = Person();
        MediaRef banner = await UploadAsync(staff, Jpeg(seed: 6), "image/jpeg", fileName: "Almighty God banner");
        await ReferencesCallAsync(references => Done(references.UseAsync("config.placement/7", [banner], Cancellation)));

        FolderInfo sermons = await FoldersCallAsync(folders => folders.CreateAsync(staff, new CreateFolderRequest { Name = "Sermons" }, Cancellation));
        FolderInfo series = await FoldersCallAsync(folders => folders.CreateAsync(staff, new CreateFolderRequest { Name = "2026 series", ParentId = sermons.Id }, Cancellation));
        await FoldersCallAsync(folders => Done(folders.MoveAsync(new MoveRequest { ItemIds = [banner.Id], Into = series.Id }, Cancellation)));
        await FoldersCallAsync(folders => Done(folders.RenameAsync(new RenameFolderRequest { Id = sermons.Id, Name = "Preaching" }, Cancellation)));
        await FoldersCallAsync(folders => Done(folders.MoveAsync(new MoveRequest { FolderIds = [series.Id] }, Cancellation)));

        MediaRef saved = Assert.Single(await ReferencesCallAsync(references => references.VerifyAsync([banner], Cancellation)));
        Assert.Equal((banner.Id, banner.Key, banner.Url, banner.Bytes), (saved.Id, saved.Key, saved.Url, saved.Bytes));

        MediaItemInfo listed = Assert.Single((await LibraryCallAsync(library => library.ListAsync(staff, new MediaQuery { FolderId = series.Id }, Cancellation))).Items);
        Assert.Equal((banner.Key, 1), (listed.Media!.Key, listed.Uses));
        MediaItemInfo found = Assert.Single((await LibraryCallAsync(library => library.ListAsync(staff, new MediaQuery { Search = "almighty" }, Cancellation))).Items);
        Assert.Equal(series.Id, found.FolderId);

        RpcException stillUsed = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => Done(library.DeleteAsync(banner.Id, Cancellation))));
        Assert.Equal(StatusCode.FailedPrecondition, stillUsed.StatusCode);

        RpcException intoItself = await Assert.ThrowsAsync<RpcException>(() =>
            FoldersCallAsync(folders => Done(folders.MoveAsync(new MoveRequest { FolderIds = [sermons.Id], Into = sermons.Id }, Cancellation))));
        Assert.Equal(StatusCode.InvalidArgument, intoItself.StatusCode);
    }

    [Fact]
    public async Task DeletingAFolderNeverDeletesTheFilesInIt()
    {
        ClaimsPrincipal staff = Person();
        FolderInfo events = await FoldersCallAsync(folders => folders.CreateAsync(staff, new CreateFolderRequest { Name = "Events" }, Cancellation));
        FolderInfo carols = await FoldersCallAsync(folders => folders.CreateAsync(staff, new CreateFolderRequest { Name = "Carols", ParentId = events.Id }, Cancellation));
        MediaRef poster = await UploadAsync(staff, Jpeg(seed: 7), "image/jpeg");
        await FoldersCallAsync(folders => Done(folders.MoveAsync(new MoveRequest { ItemIds = [poster.Id], Into = carols.Id }, Cancellation)));

        RpcException notEmpty = await Assert.ThrowsAsync<RpcException>(() =>
            FoldersCallAsync(folders => Done(folders.DeleteAsync(new DeleteFolderRequest { Id = carols.Id }, Cancellation))));
        Assert.Equal(StatusCode.FailedPrecondition, notEmpty.StatusCode);

        await FoldersCallAsync(folders => Done(folders.DeleteAsync(new DeleteFolderRequest { Id = events.Id, MoveContents = true }, Cancellation)));

        FolderTree tree = await FoldersCallAsync(folders => folders.ListAsync(staff, Cancellation));
        FolderInfo kept = Assert.Single(tree.Folders, folder => folder.Id == carols.Id);
        Assert.Null(kept.ParentId);
        Assert.Equal(1, kept.Files);
        Assert.DoesNotContain(tree.Folders, folder => folder.Id == events.Id);
    }

    [Fact]
    public async Task SystemFoldersAreKeptByTheAppAndOnlyTheirAudienceOrOversightSeesInside()
    {
        ClaimsPrincipal staff = Person();
        ClaimsPrincipal member = Person();
        ClaimsPrincipal pastor = Person();
        _access.Grant(member, ReadFiles.Node, Ministry);
        _access.Grant(pastor, MediaPermissions.SystemViewNode, null);
        MediaRef shared = await UploadAsync(member, Jpeg(seed: 8), "image/jpeg", privateTo: new MediaAudience { Permission = ReadFiles.Node, ScopeId = Ministry }, fileName: "Camp photo");
        MediaRef staffs = await UploadAsync(staff, Jpeg(seed: 9), "image/jpeg");
        await FoldersCallAsync(folders => Done(folders.FileAsync(Chats, Ministry, "Youth group", [shared], Cancellation)));

        FolderTree seenByStaff = await FoldersCallAsync(folders => folders.ListAsync(staff, Cancellation));
        FolderInfo chats = Assert.Single(seenByStaff.Folders);
        Assert.True(chats.IsSystem);
        Assert.Equal(Chats.Caption, chats.Caption);
        FolderInfo youth = Assert.Single((await FoldersCallAsync(folders => folders.ListAsync(member, Cancellation))).Folders, folder => folder.ParentId == chats.Id);
        Assert.True(youth.IsSystem);

        RpcException hidden = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => library.ListAsync(staff, new MediaQuery { FolderId = youth.Id }, Cancellation)));
        Assert.Equal(StatusCode.PermissionDenied, hidden.StatusCode);
        Assert.Empty((await LibraryCallAsync(library => library.ListAsync(staff, new MediaQuery { Search = "camp" }, Cancellation))).Items);
        foreach (ClaimsPrincipal allowed in new[] { member, pastor })
            Assert.Single((await LibraryCallAsync(library => library.ListAsync(allowed, new MediaQuery { FolderId = youth.Id }, Cancellation))).Items);

        MoveRequest[] refusedMoves =
        [
            new() { ItemIds = [shared.Id] },
            new() { ItemIds = [staffs.Id], Into = youth.Id },
            new() { FolderIds = [youth.Id] },
        ];
        foreach (MoveRequest move in refusedMoves)
        {
            RpcException refused = await Assert.ThrowsAsync<RpcException>(() => FoldersCallAsync(folders => Done(folders.MoveAsync(move, Cancellation))));
            Assert.Equal(StatusCode.FailedPrecondition, refused.StatusCode);
        }
        RpcException renamed = await Assert.ThrowsAsync<RpcException>(() =>
            FoldersCallAsync(folders => Done(folders.RenameAsync(new RenameFolderRequest { Id = chats.Id, Name = "Old chats" }, Cancellation))));
        Assert.Equal(StatusCode.FailedPrecondition, renamed.StatusCode);
        RpcException deleted = await Assert.ThrowsAsync<RpcException>(() =>
            FoldersCallAsync(folders => Done(folders.DeleteAsync(new DeleteFolderRequest { Id = youth.Id, MoveContents = true }, Cancellation))));
        Assert.Equal(StatusCode.FailedPrecondition, deleted.StatusCode);
    }

    private async Task<MediaRef> UploadAsync(ClaimsPrincipal user, byte[] bytes, string contentType, MediaAudience? privateTo = null, string fileName = "file")
    {
        UploadTicket ticket = await LibraryCallAsync(library => library.BeginUploadAsync(user, new BeginUploadRequest
        {
            FileName = fileName,
            ContentType = contentType,
            Bytes = bytes.Length,
            PrivateTo = privateTo,
        }, Cancellation));

        using ByteArrayContent body = new(bytes);
        body.Headers.ContentType = new MediaTypeHeaderValue(ticket.ContentType);
        using HttpResponseMessage put = await _http.PutAsync(ticket.Url, body, Cancellation);
        put.EnsureSuccessStatusCode();

        return await LibraryCallAsync(library => library.CompleteUploadAsync(user, ticket.UploadId, Cancellation));
    }

    private async Task<T> FoldersCallAsync<T>(Func<MediaFolders, Task<T>> call)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<MediaFolders>());
    }

    private async Task<T> LibraryCallAsync<T>(Func<MediaLibrary, Task<T>> call)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<MediaLibrary>());
    }

    private async Task<T> ReferencesCallAsync<T>(Func<IMediaReferences, Task<T>> call)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<IMediaReferences>());
    }

    private static async Task<bool> Done(Task task)
    {
        await task;
        return true;
    }

    private string PublicUrlBase => $"http://{_s3.Hostname}:{_s3.GetMappedPublicPort(S3Port)}/{PublicBucket}";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static ClaimsPrincipal Person() =>
        new(new ClaimsIdentity([new Claim(Caller.UserIdClaim, Guid.CreateVersion7().ToString())], "test"));

    private static byte[] Jpeg(int seed)
    {
        byte[] bytes = new byte[2048];
        new Random(seed).NextBytes(bytes);
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;
        return bytes;
    }

    private static byte[] Png(int width, int height)
    {
        byte[] bytes = new byte[64];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13 }.CopyTo(bytes, 0);
        "IHDR"u8.CopyTo(bytes.AsSpan(12));
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20), (uint)height);
        return bytes;
    }

    private static byte[] JpegWithOrientation(int width, int height, ushort orientation)
    {
        List<byte> bytes = [0xFF, 0xD8];
        byte[] exif = [.. "Exif\0\0"u8, .. "II"u8, 42, 0, 8, 0, 0, 0, 1, 0, 0x12, 0x01, 3, 0, 1, 0, 0, 0, (byte)orientation, 0, 0, 0, 0, 0, 0, 0];
        bytes.AddRange([0xFF, 0xE1, (byte)((exif.Length + 2) >> 8), (byte)(exif.Length + 2)]);
        bytes.AddRange(exif);
        bytes.AddRange([0xFF, 0xC0, 0, 17, 8, (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width, 3]);
        bytes.AddRange(new byte[9]);
        bytes.AddRange([0xFF, 0xD9]);
        return [.. bytes];
    }

    public async ValueTask DisposeAsync()
    {
        _http.Dispose();
        await _services.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _s3.DisposeAsync().AsTask());
    }

    private sealed class GrantedEvaluator : IAccessEvaluator
    {
        private readonly HashSet<(string User, string Node, string? Scope)> _grants = [];

        public void Grant(ClaimsPrincipal user, string node, string? scope) => _grants.Add((IdOf(user), node, scope));

        public void Revoke(ClaimsPrincipal user, string node, string? scope) => _grants.Remove((IdOf(user), node, scope));

        public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(_grants.Contains((IdOf(user), node, scopeId)));

        public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult(GrantSet.Empty);

        private static string IdOf(ClaimsPrincipal user) => user.FindFirstValue(Caller.UserIdClaim)!;
    }
}

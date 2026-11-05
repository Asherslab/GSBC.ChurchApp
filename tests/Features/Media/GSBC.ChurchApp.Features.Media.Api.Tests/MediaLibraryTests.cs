using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
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
    private readonly TileHolder _tiles = new();
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
            .AddScoped<MediaSizer>()
            .AddScoped<MediaHolders>()
            .AddSingleton<IMediaHolder>(_tiles)
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
    public async Task UploadingAFileTheLibraryAlreadyHasGivesBackTheOneItHas()
    {
        ClaimsPrincipal staff = Person();
        _access.Grant(staff, MediaPermissions.ViewNode, null);
        byte[] graphic = Jpeg(seed: 10);
        MediaRef first = await UploadAsync(staff, graphic, "image/jpeg", fileName: "Series graphic");

        MediaMatches before = await LibraryCallAsync(library => library.FindByHashAsync(staff, new FindByHashRequest
        {
            Sha256 = Convert.ToHexString(SHA256.HashData(graphic)),
            ContentType = "image/jpeg",
        }, Cancellation));
        UploadedMedia again = await UploadAgainAsync(staff, graphic, "image/jpeg", fileName: "Series graphic (1)");

        Assert.Equal(first.Id, Assert.Single(before.Items).Media!.Id);
        Assert.True(again.AlreadyInLibrary);
        Assert.Equal(first.Id, again.Item!.Media!.Id);
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        MediaDbContext db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        MediaItem only = Assert.Single(await db.Items.ToListAsync(Cancellation));
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(graphic)), only.Sha256);
        Assert.Empty(await db.PendingUploads.ToListAsync(Cancellation));
    }

    [Fact]
    public async Task AddingAFileAgainAnywayMakesASecondEntryForTheSameStoredBytes()
    {
        ClaimsPrincipal staff = Person();
        byte[] graphic = Jpeg(seed: 11);
        MediaRef first = await UploadAsync(staff, graphic, "image/jpeg");

        UploadedMedia second = await UploadAgainAsync(staff, graphic, "image/jpeg", addAgain: true);

        Assert.False(second.AlreadyInLibrary);
        Assert.NotEqual(first.Id, second.Item!.Media!.Id);
        Assert.Equal(first.Key, second.Item.Media.Key);
        ListObjectsV2Response stored = await _services.GetRequiredService<IAmazonS3>().ListObjectsV2Async(new ListObjectsV2Request { BucketName = PublicBucket }, Cancellation);
        Assert.Equal(first.Key, Assert.Single(stored.S3Objects).Key);
    }

    [Fact]
    public async Task AMatchTheCallerCanNotSeeIsNeverRevealed()
    {
        ClaimsPrincipal leader = Person();
        ClaimsPrincipal staff = Person();
        ClaimsPrincipal uploaderOnly = Person();
        _access.Grant(leader, ReadFiles.Node, Ministry);
        _access.Grant(staff, MediaPermissions.ViewNode, null);
        MediaAudience group = new() { Permission = ReadFiles.Node, ScopeId = Ministry };
        byte[] notes = "%PDF-1.7 the leaders' notes"u8.ToArray();
        byte[] poster = Jpeg(seed: 12);
        MediaRef privateNotes = await UploadAsync(leader, notes, "application/pdf", group);
        await UploadAsync(staff, poster, "image/jpeg");

        Assert.Empty(await FindAsync(staff, notes, "application/pdf"));
        Assert.Empty(await FindAsync(staff, notes, "application/pdf", group));
        Assert.Empty(await FindAsync(uploaderOnly, poster, "image/jpeg"));
        Assert.Single(await FindAsync(staff, poster, "image/jpeg"));

        byte[] campPhoto = Jpeg(seed: 13);
        MediaRef keptByTheApp = await UploadAsync(leader, campPhoto, "image/jpeg");
        await FoldersCallAsync(folders => Done(folders.FileAsync(Chats, Ministry, "Youth group", [keptByTheApp], Cancellation)));
        Assert.Empty(await FindAsync(staff, campPhoto, "image/jpeg"));
        Assert.Empty(await FindAsync(leader, campPhoto, "image/jpeg"));

        UploadedMedia staffCopy = await UploadAgainAsync(staff, notes, "application/pdf");
        Assert.False(staffCopy.AlreadyInLibrary);
        Assert.NotEqual(privateNotes.Id, staffCopy.Item!.Media!.Id);
        Assert.False(staffCopy.Item.Media.IsPrivate);
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

        MediaLink link = await LibraryCallAsync(library => library.GetLinkAsync(member, new LinkRequest { Id = file.Id }, Cancellation));
        Assert.Equal(notes, await _http.GetByteArrayAsync(link.Url, Cancellation));

        _access.Revoke(member, ReadFiles.Node, Ministry);
        RpcException removed = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => library.GetLinkAsync(member, new LinkRequest { Id = file.Id }, Cancellation)));
        RpcException never = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => library.GetLinkAsync(outsider, new LinkRequest { Id = file.Id }, Cancellation)));
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
        MediaRef sameBytes = (await UploadAgainAsync(staff, bytes, "image/jpeg", addAgain: true)).Item!.Media!;
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
    public async Task AFileTheAppKeepsIsStoredOnceAndOutlivesAStaffCopyOfTheSameBytes()
    {
        ClaimsPrincipal staff = Person();
        byte[] bytes = Jpeg(seed: 11);
        KeptFile file = new(bytes, "image/jpeg", "jpg", "artwork-large.jpg", 64, 48);
        MediaRef kept = await FoldersCallAsync(folders => folders.KeepAsync(Chats, file, Cancellation));
        MediaRef keptAgain = await FoldersCallAsync(folders => folders.KeepAsync(Chats, file, Cancellation));
        Assert.Equal(kept.Id, keptAgain.Id);

        MediaRef staffCopy = await UploadAsync(staff, bytes, "image/jpeg");
        Assert.Equal(kept.Key, staffCopy.Key);
        await LibraryCallAsync(library => Done(library.DeleteAsync(staffCopy.Id, Cancellation)));
        using (HttpResponseMessage stillThere = await _http.GetAsync(kept.Url, Cancellation))
            Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => Done(library.DeleteAsync(kept.Id, Cancellation))));
        Assert.Equal(StatusCode.FailedPrecondition, refused.StatusCode);
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
        RpcException deletedFile = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => Done(library.DeleteAsync(shared.Id, Cancellation))));
        Assert.Equal(StatusCode.FailedPrecondition, deletedFile.StatusCode);
        RpcException renamed = await Assert.ThrowsAsync<RpcException>(() =>
            FoldersCallAsync(folders => Done(folders.RenameAsync(new RenameFolderRequest { Id = chats.Id, Name = "Old chats" }, Cancellation))));
        Assert.Equal(StatusCode.FailedPrecondition, renamed.StatusCode);
        RpcException deleted = await Assert.ThrowsAsync<RpcException>(() =>
            FoldersCallAsync(folders => Done(folders.DeleteAsync(new DeleteFolderRequest { Id = youth.Id, MoveContents = true }, Cancellation))));
        Assert.Equal(StatusCode.FailedPrecondition, deleted.StatusCode);
    }

    [Fact]
    public async Task APicturesSizesAreMadeOnceAndSharedByEveryEntryOfTheSameBytes()
    {
        ClaimsPrincipal staff = Person();
        byte[] photo = Picture(3000, 1688);

        UploadedMedia first = await UploadAgainAsync(staff, photo, "image/jpeg");
        UploadedMedia second = await UploadAgainAsync(staff, photo, "image/jpeg", addAgain: true);
        MediaRef kept = await FoldersCallAsync(folders => folders.KeepAsync(Chats, new KeptFile(photo, "image/jpeg", "jpg", "kept.jpg", 3000, 1688), Cancellation));
        MediaRef small = await UploadAsync(staff, Picture(400, 300), "image/jpeg");

        MediaRef media = first.Item!.Media!;
        Assert.Equal(PictureSizing.Made, first.Item.Sizing);
        Assert.Equal([(MediaSizeName.ForPhones, 1280, 720), (MediaSizeName.Thumbnail, 480, 270)], media.Sizes.Select(size => (size.Name, size.Width, size.Height)));
        Assert.Equal(media.Sizes.Select(size => size.Key), second.Item!.Media!.Sizes.Select(size => size.Key));
        Assert.Equal(media.Sizes.Select(size => size.Key), kept.Sizes.Select(size => size.Key));
        Assert.Empty(small.Sizes);
        Assert.Equal(media.Sizes[0].Key, media.At(MediaSizeName.ForPhones).Key);
        Assert.Equal(media.Sizes[1].Key, media.At(MediaSizeName.Thumbnail).Key);
        Assert.Equal(small.Key, small.At(MediaSizeName.ForPhones).Key);

        ListObjectsV2Response stored = await _services.GetRequiredService<IAmazonS3>().ListObjectsV2Async(new ListObjectsV2Request { BucketName = PublicBucket }, Cancellation);
        Assert.Equal(4, stored.S3Objects.Count);
        foreach (MediaSize size in media.Sizes)
            Assert.Equal(size.Key, $"{Convert.ToHexStringLower(SHA256.HashData(await _http.GetByteArrayAsync(size.Url, Cancellation)))}.jpg");
    }

    [Fact]
    public async Task SizesShowTheWholePictureTurnedTheWayItIsShownAndNeverCropIt()
    {
        ClaimsPrincipal staff = Person();
        MediaRef wide = await UploadAsync(staff, Picture(3000, 1688), "image/jpeg");
        MediaRef sideways = await UploadAsync(staff, WithOrientation(Picture(2000, 1000), orientation: 6), "image/jpeg");

        foreach (MediaSize size in wide.Sizes)
        {
            using SKBitmap shown = SKBitmap.Decode(await _http.GetByteArrayAsync(size.Url, Cancellation));
            Assert.Equal((size.Width, size.Height), (shown.Width, shown.Height));
            Assert.Equal(3000d / 1688, (double)shown.Width / shown.Height, 0.01);
            Assert.Equal(Corners, CornersOf(shown));
        }

        MediaSize thumbnail = Assert.Single(sideways.Sizes);
        Assert.Equal((MediaSizeName.Thumbnail, 480, 960), (thumbnail.Name, thumbnail.Width, thumbnail.Height));
        using SKBitmap turned = SKBitmap.Decode(await _http.GetByteArrayAsync(thumbnail.Url, Cancellation));
        Assert.Equal([Corners[2], Corners[0], Corners[3], Corners[1]], CornersOf(turned));
    }

    [Fact]
    public async Task RemovingAnOriginalKeepsItsSizesAndEverywhereThatUsesThePicture()
    {
        ClaimsPrincipal staff = Person();
        byte[] photo = Picture(3000, 1688);
        MediaRef uploaded = await UploadAsync(staff, photo, "image/jpeg");
        MediaRef placed = Assert.Single(await ReferencesCallAsync(references => references.VerifyAsync([uploaded], Cancellation)));
        await ReferencesCallAsync(references => Done(references.UseAsync(TileHolder.Tile, [placed], Cancellation)));

        MediaItemInfo removed = await LibraryCallAsync(library => library.RemoveOriginalAsync(placed.Id, Cancellation));

        Assert.True(removed.Original!.Removed);
        Assert.Equal(photo.Length, removed.Original.Bytes);
        Assert.Equal(MediaSizeName.ForPhones, removed.Media!.Version);
        Assert.Equal(MediaSizeName.Thumbnail, Assert.Single(removed.Media.Sizes).Name);
        Assert.Contains(TileHolder.Tile, _tiles.Refreshed);
        using (HttpResponseMessage original = await _http.GetAsync(placed.Url, Cancellation))
            Assert.False(original.IsSuccessStatusCode);

        MediaRef stillPlaced = Assert.Single(await ReferencesCallAsync(references => references.VerifyAsync([placed], Cancellation)));
        Assert.Equal(removed.Media.Key, stillPlaced.Key);
        Assert.Equal(placed.AltText, stillPlaced.AltText);
        foreach (string? url in new[] { stillPlaced.Url, stillPlaced.At(MediaSizeName.Thumbnail).Url })
        {
            using HttpResponseMessage shown = await _http.GetAsync(url, Cancellation);
            Assert.Equal(HttpStatusCode.OK, shown.StatusCode);
        }

        MediaRef inADraft = await UploadAsync(staff, Picture(2000, 1500), "image/jpeg");
        await ReferencesCallAsync(references => Done(references.UseAsync("draft/1", [inADraft], Cancellation)));
        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => library.RemoveOriginalAsync(inADraft.Id, Cancellation)));
        Assert.Equal(StatusCode.FailedPrecondition, refused.StatusCode);
    }

    [Fact]
    public async Task UploadingARemovedOriginalAgainOffersToPutItBackAndPuttingItBackMakesThePictureWhole()
    {
        ClaimsPrincipal staff = Person();
        _access.Grant(staff, MediaPermissions.ViewNode, null);
        byte[] photo = Picture(3000, 1688);
        MediaRef picture = await UploadAsync(staff, photo, "image/jpeg");
        await ReferencesCallAsync(references => Done(references.UseAsync(TileHolder.Tile, [picture], Cancellation)));
        await LibraryCallAsync(library => library.RemoveOriginalAsync(picture.Id, Cancellation));
        _tiles.Refreshed.Clear();

        MediaMatches offered = await LibraryCallAsync(library => library.FindByHashAsync(staff, new FindByHashRequest { Sha256 = Sha256Of(photo), ContentType = "image/jpeg" }, Cancellation));
        UploadedMedia putBack = await UploadAgainAsync(staff, photo, "image/jpeg");

        Assert.Empty(offered.Items);
        Assert.Equal(picture.Id, Assert.Single(offered.RemovedOriginals).Media!.Id);
        Assert.True(putBack.PutBack);
        Assert.Equal(picture.Id, putBack.Item!.Media!.Id);
        Assert.False(putBack.Item.Original!.Removed);
        Assert.Equal(MediaSizeName.Original, putBack.Item.Media.Version);
        Assert.Equal(photo, await _http.GetByteArrayAsync(picture.Url, Cancellation));
        Assert.Contains(TileHolder.Tile, _tiles.Refreshed);

        await LibraryCallAsync(library => library.RemoveOriginalAsync(picture.Id, Cancellation));
        UploadedMedia asNew = await UploadAgainAsync(staff, photo, "image/jpeg", addAgain: true);
        Assert.False(asNew.PutBack);
        Assert.NotEqual(picture.Id, asNew.Item!.Media!.Id);
    }

    [Fact]
    public async Task ARemovedOriginalIsOnlyOfferedBackToPeopleWhoCanSeeItsPicture()
    {
        ClaimsPrincipal staff = Person();
        ClaimsPrincipal uploaderOnly = Person();
        _access.Grant(staff, MediaPermissions.ViewNode, null);
        byte[] photo = Picture(3000, 1688);
        MediaRef picture = await UploadAsync(staff, photo, "image/jpeg");
        await LibraryCallAsync(library => library.RemoveOriginalAsync(picture.Id, Cancellation));

        MediaMatches offered = await LibraryCallAsync(library => library.FindByHashAsync(uploaderOnly, new FindByHashRequest { Sha256 = Sha256Of(photo), ContentType = "image/jpeg" }, Cancellation));
        RpcException named = await Assert.ThrowsAsync<RpcException>(() => PutBackIntoAsync(uploaderOnly, photo, picture.Id));
        UploadedMedia theirs = await UploadAgainAsync(uploaderOnly, photo, "image/jpeg");

        Assert.Empty(offered.Items);
        Assert.Empty(offered.RemovedOriginals);
        Assert.False(theirs.PutBack);
        Assert.NotEqual(picture.Id, theirs.Item!.Media!.Id);
        Assert.Equal(StatusCode.NotFound, named.StatusCode);
        MediaItemInfo stillRemoved = Assert.Single((await LibraryCallAsync(library => library.FindByHashAsync(staff,
            new FindByHashRequest { Sha256 = Sha256Of(photo), ContentType = "image/jpeg" }, Cancellation))).RemovedOriginals);
        Assert.Equal(picture.Id, stillRemoved.Media!.Id);
    }

    [Fact]
    public async Task SermonArtworkPairsBecomeOnePictureWithoutBreakingWhatSermonsHolds()
    {
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            MediaDbContext db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
            await db.GetService<IMigrator>().MigrateAsync("20261104155719_MediaContentHash", cancellationToken: Cancellation);
            Guid nobody = Guid.Empty;
            string largeKey = $"{new string('a', 64)}.jpg", smallKey = $"{new string('b', 64)}.jpg";
            string largeHash = new('a', 64), smallHash = new('b', 64);
            string twiceLargeHash = new('c', 64), twiceLargeAgainHash = new('d', 64), twiceSmallHash = new('e', 64);
            string twiceLargeKey = $"{twiceLargeHash}.jpg", twiceLargeAgainKey = $"{twiceLargeAgainHash}.jpg", twiceSmallKey = $"{twiceSmallHash}.jpg";
            await db.Database.ExecuteSqlAsync($"INSERT INTO media.folders (id, name, system_area, created_by, created_at) VALUES ({ArtworkFolder}, 'Sermon artwork', 'sermons', {nobody}, now())", Cancellation);
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO media.items (id, key, sha256, content_type, bytes, width, height, file_name, alt_text, uploaded_by, uploaded_at, folder_id) VALUES
                    ({LargeArtwork}, {largeKey}, {largeHash}, 'image/jpeg', 200000, 1280, 720, '0123456789ab-large.jpg', '', {nobody}, now(), {ArtworkFolder}),
                    ({SmallArtwork}, {smallKey}, {smallHash}, 'image/jpeg', 40000, 480, 270, '0123456789ab-small.jpg', '', {nobody}, now(), {ArtworkFolder}),
                    ({TwiceLarge}, {twiceLargeKey}, {twiceLargeHash}, 'image/jpeg', 210000, 1280, 720, 'fedcba987654-large.jpg', '', {nobody}, now(), {ArtworkFolder}),
                    ({TwiceLargeAgain}, {twiceLargeAgainKey}, {twiceLargeAgainHash}, 'image/jpeg', 215000, 1280, 720, 'fedcba987654-large.jpg', '', {nobody}, now(), {ArtworkFolder}),
                    ({TwiceSmall}, {twiceSmallKey}, {twiceSmallHash}, 'image/jpeg', 41000, 480, 270, 'fedcba987654-small.jpg', '', {nobody}, now(), {ArtworkFolder})
                """, Cancellation);
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO media.uses (media_id, owner) VALUES
                    ({LargeArtwork}, 'sermons/sermon/1/artwork'), ({SmallArtwork}, 'sermons/sermon/1/artwork'), ({SmallArtwork}, 'sermons/sermon/2/artwork')
                """, Cancellation);
            await db.Database.MigrateAsync(Cancellation);
        }

        MediaRef small = new() { Id = SmallArtwork.ToString(), Key = $"{new string('b', 64)}.jpg" };
        MediaRef large = new() { Id = LargeArtwork.ToString(), Key = $"{new string('a', 64)}.jpg" };
        IReadOnlyList<MediaRef> verified = await ReferencesCallAsync(references => references.VerifyAsync([large, small], Cancellation));
        await ReferencesCallAsync(references => Done(references.UseAsync("sermons/sermon/3/artwork", [large, small], Cancellation)));

        await using AsyncServiceScope after = _services.CreateAsyncScope();
        MediaDbContext merged = after.ServiceProvider.GetRequiredService<MediaDbContext>();
        Assert.Equal(new[] { LargeArtwork, TwiceLarge, TwiceLargeAgain, TwiceSmall }.Order(), (await merged.Items.Select(item => item.Id).ToListAsync(Cancellation)).Order());
        MediaRendition thumbnail = Assert.Single(await merged.Renditions.ToListAsync(Cancellation));
        Assert.Equal((LargeArtwork, small.Key), (thumbnail.MediaId, thumbnail.Key));
        Assert.All(verified, media => Assert.Equal((LargeArtwork.ToString(), large.Key), (media.Id, media.Key)));
        Assert.Equal(small.Key, verified[1].At(MediaSizeName.Thumbnail).Key);
        Assert.Equal(["sermons/sermon/1/artwork", "sermons/sermon/2/artwork", "sermons/sermon/3/artwork"],
            (await merged.Uses.Where(use => use.MediaId == LargeArtwork).Select(use => use.Owner).ToListAsync(Cancellation)).Order());
    }

    private async Task<MediaRef> UploadAsync(ClaimsPrincipal user, byte[] bytes, string contentType, MediaAudience? privateTo = null, string fileName = "file") =>
        (await UploadAgainAsync(user, bytes, contentType, privateTo, fileName)).Item!.Media!;

    private async Task<UploadedMedia> UploadAgainAsync(
        ClaimsPrincipal user, byte[] bytes, string contentType, MediaAudience? privateTo = null, string fileName = "file", bool addAgain = false)
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

        return await LibraryCallAsync(library => library.CompleteUploadAsync(user, new CompleteUploadRequest { UploadId = ticket.UploadId, AddAgain = addAgain }, Cancellation));
    }

    private async Task<UploadedMedia> PutBackIntoAsync(ClaimsPrincipal user, byte[] bytes, string mediaId)
    {
        UploadTicket ticket = await LibraryCallAsync(library => library.BeginUploadAsync(user, new BeginUploadRequest { FileName = "again.jpg", ContentType = "image/jpeg", Bytes = bytes.Length }, Cancellation));
        using ByteArrayContent body = new(bytes);
        body.Headers.ContentType = new MediaTypeHeaderValue(ticket.ContentType);
        using HttpResponseMessage put = await _http.PutAsync(ticket.Url, body, Cancellation);
        put.EnsureSuccessStatusCode();
        return await LibraryCallAsync(library => library.CompleteUploadAsync(user, new CompleteUploadRequest { UploadId = ticket.UploadId, PutBackInto = mediaId }, Cancellation));
    }

    private async Task<List<MediaItemInfo>> FindAsync(ClaimsPrincipal user, byte[] bytes, string contentType, MediaAudience? privateTo = null) =>
        (await LibraryCallAsync(library => library.FindByHashAsync(user, new FindByHashRequest
        {
            Sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)),
            ContentType = contentType,
            PrivateTo = privateTo,
        }, Cancellation))).Items;

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

    private static readonly Guid ArtworkFolder = Guid.CreateVersion7();
    private static readonly Guid LargeArtwork = Guid.CreateVersion7();
    private static readonly Guid SmallArtwork = Guid.CreateVersion7();
    private static readonly Guid TwiceLarge = Guid.CreateVersion7();
    private static readonly Guid TwiceLargeAgain = Guid.CreateVersion7();
    private static readonly Guid TwiceSmall = Guid.CreateVersion7();

    private static readonly SKColor[] Corners = [SKColors.Red, SKColors.Lime, SKColors.Blue, SKColors.Black];

    private static string Sha256Of(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static byte[] Picture(int width, int height)
    {
        using SKBitmap bitmap = new(width, height);
        using (SKCanvas canvas = new(bitmap))
        {
            canvas.Clear(SKColors.White);
            int corner = Math.Max(width, height) / 10;
            SKPoint[] origins = [new(0, 0), new(width - corner, 0), new(0, height - corner), new(width - corner, height - corner)];
            for (int index = 0; index < Corners.Length; index++)
            {
                using SKPaint paint = new();
                paint.Color = Corners[index];
                canvas.DrawRect(SKRect.Create(origins[index], new SKSize(corner, corner)), paint);
            }
        }
        using SKData data = bitmap.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    private static SKColor[] CornersOf(SKBitmap shown)
    {
        int inset = Math.Max(2, Math.Min(shown.Width, shown.Height) / 40);
        SKColor[] corners =
        [
            shown.GetPixel(inset, inset), shown.GetPixel(shown.Width - 1 - inset, inset),
            shown.GetPixel(inset, shown.Height - 1 - inset), shown.GetPixel(shown.Width - 1 - inset, shown.Height - 1 - inset),
        ];
        return corners.Select(Nearest).ToArray();
    }

    private static SKColor Nearest(SKColor color) =>
        Corners.Append(SKColors.White).MinBy(known => Math.Abs(known.Red - color.Red) + Math.Abs(known.Green - color.Green) + Math.Abs(known.Blue - color.Blue));

    private static byte[] WithOrientation(byte[] jpeg, ushort orientation)
    {
        byte[] exif = [.. "Exif\0\0"u8, .. "II"u8, 42, 0, 8, 0, 0, 0, 1, 0, 0x12, 0x01, 3, 0, 1, 0, 0, 0, (byte)orientation, 0, 0, 0, 0, 0, 0, 0];
        return [0xFF, 0xD8, 0xFF, 0xE1, (byte)((exif.Length + 2) >> 8), (byte)(exif.Length + 2), .. exif, .. jpeg.AsSpan(2)];
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

    private sealed class TileHolder : IMediaHolder
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

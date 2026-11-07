using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

public sealed class MediaUploadTests : MediaLibraryHost
{
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

        using HttpResponseMessage served = await Http.GetAsync(first.Url, Cancellation);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal(photo, await served.Content.ReadAsByteArrayAsync(Cancellation));
        Assert.Equal("image/jpeg", served.Content.Headers.ContentType?.MediaType);
        Assert.Equal(ContentAddress.ImmutableCacheControl, served.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task UploadingAFileTheLibraryAlreadyHasGivesBackTheOneItHas()
    {
        ClaimsPrincipal staff = Person();
        Access.Grant(staff, MediaPermissions.ViewNode, null);
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
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
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
        ListObjectsV2Response stored = await Services.GetRequiredService<IAmazonS3>().ListObjectsV2Async(new ListObjectsV2Request { BucketName = Buckets.Public }, Cancellation);
        Assert.Equal(first.Key, Assert.Single(stored.S3Objects).Key);
    }

    [Fact]
    public async Task AMatchTheCallerCanNotSeeIsNeverRevealed()
    {
        ClaimsPrincipal leader = Person();
        ClaimsPrincipal staff = Person();
        ClaimsPrincipal uploaderOnly = Person();
        Access.Grant(leader, ReadFiles.Node, Ministry);
        Access.Grant(staff, MediaPermissions.ViewNode, null);
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
            UploadAsync(staff, "<html><script>alert(1)</script></html>"u8.ToArray(), "image/png"));

        Assert.Equal(StatusCode.InvalidArgument, refused.StatusCode);
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        MediaDbContext db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        Assert.Empty(await db.Items.ToListAsync(Cancellation));
        Assert.Empty(await db.PendingUploads.ToListAsync(Cancellation));
    }

    [Fact]
    public async Task OnlyPeopleTrustedWithFontLicencesCanUploadAFontFile()
    {
        ClaimsPrincipal staff = Person();
        byte[] font = [.. "wOF2"u8, .. new byte[64]];

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => UploadAsync(staff, font, "font/woff2"));
        Access.Grant(staff, MediaPermissions.UploadFontsNode, null);
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
}

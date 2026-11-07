using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

public sealed class PictureSizeTests : MediaLibraryHost
{
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

        ListObjectsV2Response stored = await Services.GetRequiredService<IAmazonS3>().ListObjectsV2Async(new ListObjectsV2Request { BucketName = Buckets.Public }, Cancellation);
        Assert.Equal(4, stored.S3Objects.Count);
        foreach (MediaSize size in media.Sizes)
            Assert.Equal(size.Key, $"{Convert.ToHexStringLower(SHA256.HashData(await Http.GetByteArrayAsync(size.Url, Cancellation)))}.jpg");
    }

    [Fact]
    public async Task SizesShowTheWholePictureTurnedTheWayItIsShownAndNeverCropIt()
    {
        ClaimsPrincipal staff = Person();
        MediaRef wide = await UploadAsync(staff, Picture(3000, 1688), "image/jpeg");
        MediaRef sideways = await UploadAsync(staff, WithOrientation(Picture(2000, 1000), orientation: 6), "image/jpeg");

        foreach (MediaSize size in wide.Sizes)
        {
            using SKBitmap shown = SKBitmap.Decode(await Http.GetByteArrayAsync(size.Url, Cancellation));
            Assert.Equal((size.Width, size.Height), (shown.Width, shown.Height));
            Assert.Equal(3000d / 1688, (double)shown.Width / shown.Height, 0.01);
            Assert.Equal(Corners, CornersOf(shown));
        }

        MediaSize thumbnail = Assert.Single(sideways.Sizes);
        Assert.Equal((MediaSizeName.Thumbnail, 480, 960), (thumbnail.Name, thumbnail.Width, thumbnail.Height));
        using SKBitmap turned = SKBitmap.Decode(await Http.GetByteArrayAsync(thumbnail.Url, Cancellation));
        Assert.Equal([Corners[2], Corners[0], Corners[3], Corners[1]], CornersOf(turned));
    }

    [Fact]
    public async Task RemovingAnOriginalKeepsItsSizesAndEverywhereThatUsesThePictureAndNeverLeavesAFullWidthScreenWithAThumbnail()
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
        Assert.Contains(TileHolder.Tile, Tiles.Refreshed);
        using (HttpResponseMessage original = await Http.GetAsync(placed.Url, Cancellation))
            Assert.False(original.IsSuccessStatusCode);

        MediaRef stillPlaced = Assert.Single(await ReferencesCallAsync(references => references.VerifyAsync([placed], Cancellation)));
        Assert.Equal(removed.Media.Key, stillPlaced.Key);
        Assert.Equal(placed.AltText, stillPlaced.AltText);
        foreach (string? url in new[] { stillPlaced.Url, stillPlaced.At(MediaSizeName.Thumbnail).Url })
        {
            using HttpResponseMessage shown = await Http.GetAsync(url, Cancellation);
            Assert.Equal(HttpStatusCode.OK, shown.StatusCode);
        }

        MediaRef inADraft = await UploadAsync(staff, Picture(2000, 1500), "image/jpeg");
        await ReferencesCallAsync(references => Done(references.UseAsync("draft/1", [inADraft], Cancellation)));
        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => library.RemoveOriginalAsync(inADraft.Id, Cancellation)));
        Assert.Equal(StatusCode.FailedPrecondition, refused.StatusCode);

        MediaRef shownFullWidth = await UploadAsync(staff, Picture(800, 450), "image/jpeg");
        RpcException kept = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => library.RemoveOriginalAsync(shownFullWidth.Id, Cancellation)));
        Assert.Equal(StatusCode.FailedPrecondition, kept.StatusCode);
    }

    [Fact]
    public async Task UploadingARemovedOriginalAgainOffersToPutItBackAndPuttingItBackMakesThePictureWhole()
    {
        ClaimsPrincipal staff = Person();
        Access.Grant(staff, MediaPermissions.ViewNode, null);
        byte[] photo = Picture(3000, 1688);
        MediaRef picture = await UploadAsync(staff, photo, "image/jpeg");
        await ReferencesCallAsync(references => Done(references.UseAsync(TileHolder.Tile, [picture], Cancellation)));
        await LibraryCallAsync(library => library.RemoveOriginalAsync(picture.Id, Cancellation));
        Tiles.Refreshed.Clear();

        MediaMatches offered = await LibraryCallAsync(library => library.FindByHashAsync(staff, new FindByHashRequest { Sha256 = Sha256Of(photo), ContentType = "image/jpeg" }, Cancellation));
        UploadedMedia putBack = await UploadAgainAsync(staff, photo, "image/jpeg");

        Assert.Empty(offered.Items);
        Assert.Equal(picture.Id, Assert.Single(offered.RemovedOriginals).Media!.Id);
        Assert.True(putBack.PutBack);
        Assert.Equal(picture.Id, putBack.Item!.Media!.Id);
        Assert.False(putBack.Item.Original!.Removed);
        Assert.Equal(MediaSizeName.Original, putBack.Item.Media.Version);
        Assert.Equal(photo, await Http.GetByteArrayAsync(picture.Url, Cancellation));
        Assert.Contains(TileHolder.Tile, Tiles.Refreshed);

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
        Access.Grant(staff, MediaPermissions.ViewNode, null);
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
        await using (AsyncServiceScope scope = Services.CreateAsyncScope())
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

        await using AsyncServiceScope after = Services.CreateAsyncScope();
        MediaDbContext merged = after.ServiceProvider.GetRequiredService<MediaDbContext>();
        Assert.Equal(new[] { LargeArtwork, TwiceLarge, TwiceLargeAgain, TwiceSmall }.Order(), (await merged.Items.Select(item => item.Id).ToListAsync(Cancellation)).Order());
        MediaRendition thumbnail = Assert.Single(await merged.Renditions.ToListAsync(Cancellation));
        Assert.Equal((LargeArtwork, small.Key), (thumbnail.MediaId, thumbnail.Key));
        Assert.Equal("0123456789ab.jpg", (await merged.Items.SingleAsync(item => item.Id == LargeArtwork, Cancellation)).FileName);
        Assert.All(verified, media => Assert.Equal((LargeArtwork.ToString(), large.Key), (media.Id, media.Key)));
        Assert.Equal(small.Key, verified[1].At(MediaSizeName.Thumbnail).Key);
        Assert.Equal(["sermons/sermon/1/artwork", "sermons/sermon/2/artwork", "sermons/sermon/3/artwork"],
            (await merged.Uses.Where(use => use.MediaId == LargeArtwork).Select(use => use.Owner).ToListAsync(Cancellation)).Order());
    }
}

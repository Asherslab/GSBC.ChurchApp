using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

[Integration("A picture's sizes are stored once under their content hashes, and removing or putting back its original touches only the original's object")]
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

        MediaRef media = first.Item!.Media!;
        Assert.Equal(2, media.Sizes.Count);
        Assert.Equal(media.Sizes.Select(size => size.Key), second.Item!.Media!.Sizes.Select(size => size.Key));
        Assert.Equal(media.Sizes.Select(size => size.Key), kept.Sizes.Select(size => size.Key));

        ListObjectsV2Response stored = await Services.GetRequiredService<IAmazonS3>().ListObjectsV2Async(new ListObjectsV2Request { BucketName = Buckets.Public }, Cancellation);
        Assert.Equal(3, stored.S3Objects.Count);
        foreach (MediaSize size in media.Sizes)
            Assert.Equal(size.Key, $"{Convert.ToHexStringLower(SHA256.HashData(await Http.GetByteArrayAsync(size.Url, Cancellation)))}.jpg");
    }

    [Fact]
    public async Task RemovingAnOriginalDeletesOnlyItsObjectAndEverywhereThatUsesThePictureMovesToItsSize()
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
    }

    [Fact]
    public async Task PuttingARemovedOriginalBackStoresItAgainAtItsOwnKey()
    {
        ClaimsPrincipal staff = Person();
        Access.Grant(staff, MediaPermissions.ViewNode, null);
        byte[] photo = Picture(3000, 1688);
        MediaRef picture = await UploadAsync(staff, photo, "image/jpeg");
        await ReferencesCallAsync(references => Done(references.UseAsync(TileHolder.Tile, [picture], Cancellation)));
        await LibraryCallAsync(library => library.RemoveOriginalAsync(picture.Id, Cancellation));
        Tiles.Refreshed.Clear();

        UploadedMedia putBack = await UploadAgainAsync(staff, photo, "image/jpeg");

        Assert.True(putBack.PutBack);
        Assert.Equal(picture.Id, putBack.Item!.Media!.Id);
        Assert.False(putBack.Item.Original!.Removed);
        Assert.Equal(MediaSizeName.Original, putBack.Item.Media.Version);
        Assert.Equal(photo, await Http.GetByteArrayAsync(picture.Url, Cancellation));
        Assert.Contains(TileHolder.Tile, Tiles.Refreshed);
    }
}

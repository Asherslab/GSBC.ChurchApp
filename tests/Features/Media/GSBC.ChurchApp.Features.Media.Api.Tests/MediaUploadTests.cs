using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Storage;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

[Integration("A file is stored under its content hash, served immutable, and every entry of the same bytes shares that one object")]
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
}

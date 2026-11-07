using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Storage;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

public sealed class MediaRefTests
{
    [Fact]
    public void ASavedMediaRefIsTheLibrarysNeverTheClients()
    {
        MediaItem photo = new()
        {
            Id = Guid.CreateVersion7(), Key = $"{new string('a', 64)}.jpg", Sha256 = new string('a', 64), ContentType = "image/jpeg", Bytes = 2048, Width = 64, Height = 48,
        };
        MediaRef forgedDetails = new()
        {
            Id = photo.Id.ToString(),
            Key = photo.Key,
            ContentType = "text/html",
            Bytes = 1,
            Url = "https://attacker.example/x.jpg",
            IsPrivate = true,
            AltText = "The hall set up for lunch",
        };

        MediaRef saved = MediaReferences.Verified(forgedDetails, photo, [], new PublicBlobs());

        Assert.Equal(("image/jpeg", 2048L, $"https://media.example/{photo.Key}", false), (saved.ContentType, saved.Bytes, saved.Url, saved.IsPrivate));
        Assert.Equal("The hall set up for lunch", saved.AltText);
        MediaRef otherKey = new() { Id = photo.Id.ToString(), Key = $"{new string('0', 64)}.jpg" };
        Assert.Equal(StatusCode.InvalidArgument, Assert.Throws<RpcException>(() => MediaReferences.Verified(otherKey, photo, [], new PublicBlobs())).StatusCode);
    }

    private sealed class PublicBlobs : IBlobStore
    {
        public Uri PublicUrlOf(string key) => new($"https://media.example/{key}");

        public Task<Uri> PresignUploadAsync(BlobBucket bucket, string key, string contentType, TimeSpan validFor) => throw new NotSupportedException();

        public Task<Uri> PresignDownloadAsync(BlobBucket bucket, string key, TimeSpan validFor) => throw new NotSupportedException();

        public Task<BlobInfo?> FindAsync(BlobBucket bucket, string key, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Stream> OpenAsync(BlobBucket bucket, string key, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task PutAsync(BlobBucket bucket, string key, Stream content, BlobHeaders headers, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task CopyAsync(BlobBucket fromBucket, string fromKey, BlobBucket toBucket, string toKey, BlobHeaders headers, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(BlobBucket bucket, string key, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

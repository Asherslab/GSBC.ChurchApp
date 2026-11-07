using System.Buffers.Binary;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
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
using SkiaSharp;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

public abstract class MediaLibraryHost : IAsyncLifetime
{
    private protected const string Ministry = "42";

    private protected static readonly Permission ReadFiles = new("ministries.files.read", ScopeKind.Ministry, "Read a ministry's files");
    private protected static readonly MediaSystemArea Chats = new("chats", "Chats", "Made by the app: files people share in group chats", ReadFiles);

    private protected TestBuckets Buckets { get; private set; } = null!;
    private TestDatabase _database = null!;
    private readonly TestClock _clock = new();
    private protected GrantedEvaluator Access { get; } = new();
    private protected TileHolder Tiles { get; } = new();
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
            .AddSingleton(new PermissionCatalog([new PermissionSet([.. MediaPermissions.All, ReadFiles])]))
            .AddSingleton<IAccessEvaluator>(Access)
            .AddBlobStore(configuration)
            .AddMediaData()
            .AddSyncData()
            .AddScoped<MediaLibrary>()
            .AddScoped<MediaSizer>()
            .AddScoped<MediaHolders>()
            .AddSingleton<IMediaHolder>(Tiles)
            .AddSingleton(Chats)
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
        UploadTicket ticket = await LibraryCallAsync(library => library.BeginUploadAsync(user, new BeginUploadRequest
        {
            FileName = fileName,
            ContentType = contentType,
            Bytes = bytes.Length,
            PrivateTo = privateTo,
        }, Cancellation));

        using ByteArrayContent body = new(bytes);
        body.Headers.ContentType = new MediaTypeHeaderValue(ticket.ContentType);
        using HttpResponseMessage put = await Http.PutAsync(ticket.Url, body, Cancellation);
        put.EnsureSuccessStatusCode();

        return await LibraryCallAsync(library => library.CompleteUploadAsync(user, new CompleteUploadRequest { UploadId = ticket.UploadId, AddAgain = addAgain }, Cancellation));
    }

    private protected async Task<UploadedMedia> PutBackIntoAsync(ClaimsPrincipal user, byte[] bytes, string mediaId)
    {
        UploadTicket ticket = await LibraryCallAsync(library => library.BeginUploadAsync(user, new BeginUploadRequest { FileName = "again.jpg", ContentType = "image/jpeg", Bytes = bytes.Length }, Cancellation));
        using ByteArrayContent body = new(bytes);
        body.Headers.ContentType = new MediaTypeHeaderValue(ticket.ContentType);
        using HttpResponseMessage put = await Http.PutAsync(ticket.Url, body, Cancellation);
        put.EnsureSuccessStatusCode();
        return await LibraryCallAsync(library => library.CompleteUploadAsync(user, new CompleteUploadRequest { UploadId = ticket.UploadId, PutBackInto = mediaId }, Cancellation));
    }

    private protected async Task<List<MediaItemInfo>> FindAsync(ClaimsPrincipal user, byte[] bytes, string contentType, MediaAudience? privateTo = null) =>
        (await LibraryCallAsync(library => library.FindByHashAsync(user, new FindByHashRequest
        {
            Sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)),
            ContentType = contentType,
            PrivateTo = privateTo,
        }, Cancellation))).Items;

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

    private protected static byte[] Jpeg(int seed)
    {
        byte[] bytes = new byte[2048];
        new Random(seed).NextBytes(bytes);
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;
        return bytes;
    }

    private protected static byte[] Png(int width, int height)
    {
        byte[] bytes = new byte[64];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13 }.CopyTo(bytes, 0);
        "IHDR"u8.CopyTo(bytes.AsSpan(12));
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20), (uint)height);
        return bytes;
    }

    private protected static readonly Guid ArtworkFolder = Guid.CreateVersion7();
    private protected static readonly Guid LargeArtwork = Guid.CreateVersion7();
    private protected static readonly Guid SmallArtwork = Guid.CreateVersion7();
    private protected static readonly Guid TwiceLarge = Guid.CreateVersion7();
    private protected static readonly Guid TwiceLargeAgain = Guid.CreateVersion7();
    private protected static readonly Guid TwiceSmall = Guid.CreateVersion7();

    private protected static readonly SKColor[] Corners = [SKColors.Red, SKColors.Lime, SKColors.Blue, SKColors.Black];

    private protected static string Sha256Of(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private protected static byte[] Picture(int width, int height)
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

    private protected static SKColor[] CornersOf(SKBitmap shown)
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

    private protected static byte[] WithOrientation(byte[] jpeg, ushort orientation)
    {
        byte[] exif = [.. "Exif\0\0"u8, .. "II"u8, 42, 0, 8, 0, 0, 0, 1, 0, 0x12, 0x01, 3, 0, 1, 0, 0, 0, (byte)orientation, 0, 0, 0, 0, 0, 0, 0];
        return [0xFF, 0xD8, 0xFF, 0xE1, (byte)((exif.Length + 2) >> 8), (byte)(exif.Length + 2), .. exif, .. jpeg.AsSpan(2)];
    }

    private protected static byte[] JpegWithOrientation(int width, int height, ushort orientation)
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
        Http.Dispose();
        await Services.DisposeAsync();
        await _database.DisposeAsync();
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

    private protected sealed class GrantedEvaluator : IAccessEvaluator
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

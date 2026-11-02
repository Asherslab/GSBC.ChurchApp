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
            .AddScoped<MediaLibrary>()
            .BuildServiceProvider();

        IAmazonS3 s3 = _services.GetRequiredService<IAmazonS3>();
        await s3.PutBucketAsync(PublicBucket);
        await s3.PutBucketAsync(PrivateBucket);

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
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

    private async Task<MediaRef> UploadAsync(ClaimsPrincipal user, byte[] bytes, string contentType, MediaAudience? privateTo = null)
    {
        UploadTicket ticket = await LibraryCallAsync(library => library.BeginUploadAsync(user, new BeginUploadRequest
        {
            FileName = "file",
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

    private async Task<T> LibraryCallAsync<T>(Func<MediaLibrary, Task<T>> call)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<MediaLibrary>());
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

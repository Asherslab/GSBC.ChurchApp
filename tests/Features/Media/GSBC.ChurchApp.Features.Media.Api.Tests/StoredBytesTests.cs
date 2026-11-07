using System.Net;
using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

[Integration("Stored bytes stay while any entry lists them, and private ones are only reachable through a short-lived presigned link")]
public sealed class StoredBytesTests : MediaLibraryHost
{
    [Fact]
    public async Task APrivateFileIsNeverPublicAndIsReachedThroughAShortLivedLink()
    {
        ClaimsPrincipal leader = Person();
        ClaimsPrincipal member = Person();
        Access.Grant(leader, ReadFiles.Node, Ministry);
        Access.Grant(member, ReadFiles.Node, Ministry);
        byte[] notes = "%PDF-1.7 the group's notes"u8.ToArray();

        MediaRef file = await UploadAsync(leader, notes, "application/pdf", new MediaAudience { Permission = ReadFiles.Node, ScopeId = Ministry });

        Assert.True(file.IsPrivate);
        Assert.Null(file.Url);
        using (HttpResponseMessage anonymous = await Http.GetAsync($"{PublicUrlBase}/{file.Key}", Cancellation))
            Assert.False(anonymous.IsSuccessStatusCode);

        MediaLink link = await LibraryCallAsync(library => library.GetLinkAsync(member, new LinkRequest { Id = file.Id }, Cancellation));
        Assert.Equal(notes, await Http.GetByteArrayAsync(link.Url, Cancellation));
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
        using (HttpResponseMessage stillListed = await Http.GetAsync(sameBytes.Url, Cancellation))
            Assert.Equal(HttpStatusCode.OK, stillListed.StatusCode);

        await LibraryCallAsync(library => Done(library.DeleteAsync(sameBytes.Id, Cancellation)));
        using (HttpResponseMessage gone = await Http.GetAsync(sameBytes.Url, Cancellation))
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
        using (HttpResponseMessage stillThere = await Http.GetAsync(kept.Url, Cancellation))
            Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => Done(library.DeleteAsync(kept.Id, Cancellation))));
        Assert.Equal(StatusCode.FailedPrecondition, refused.StatusCode);
    }
}

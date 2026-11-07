using System.Net;
using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

public sealed class MediaPrivacyAndFolderTests : MediaLibraryHost
{
    [Fact]
    public async Task PrivateMediaIsOnlyLinkedWhileTheCallerStillHoldsItsAudience()
    {
        ClaimsPrincipal leader = Person();
        ClaimsPrincipal member = Person();
        ClaimsPrincipal outsider = Person();
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

        Access.Revoke(member, ReadFiles.Node, Ministry);
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
        Access.Grant(member, ReadFiles.Node, Ministry);
        Access.Grant(pastor, MediaPermissions.SystemViewNode, null);
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
}

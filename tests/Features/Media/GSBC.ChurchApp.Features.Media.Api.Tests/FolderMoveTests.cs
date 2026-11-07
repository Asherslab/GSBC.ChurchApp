using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

[Integration("Folders are rows files point at, so moving or deleting them, in one transaction, never changes a file's id, key, uses or bytes")]
public sealed class FolderMoveTests : MediaLibraryHost
{
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
}

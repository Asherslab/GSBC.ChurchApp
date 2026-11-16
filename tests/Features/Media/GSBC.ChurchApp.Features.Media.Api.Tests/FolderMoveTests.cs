using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task ALibraryFileAPostUsesStaysInTheLibraryAndOnlyTheAreasOwnUploadsAreFiled()
    {
        ClaimsPrincipal staff = Person(), member = Person();
        FolderInfo mainImages = await FoldersCallAsync(folders => folders.CreateAsync(staff, new CreateFolderRequest { Name = "Main Images" }, Cancellation));
        MediaRef letterboxing = await UploadAsync(staff, Jpeg(seed: 41), "image/jpeg", fileName: "letterboxing.jpeg");
        await FoldersCallAsync(folders => Done(folders.MoveAsync(new MoveRequest { ItemIds = [letterboxing.Id], Into = mainImages.Id }, Cancellation)));
        Members.Add(member, "chat-9");
        MediaRef shared = (await SendAsync(member, Jpeg(seed: 42), new BeginUploadRequest
        {
            FileName = "camp.jpg", ContentType = "image/jpeg", Bytes = Jpeg(seed: 42).Length, Area = Chats.Key, AreaScopeId = "chat-9",
        })).Item!.Media!;
        await ReferencesCallAsync(references => Done(references.UseAsync("chats.message/1", [letterboxing, shared], Cancellation)));

        await FoldersCallAsync(folders => Done(folders.FileAsync(Chats, "chat-9", "Youth group", [letterboxing, shared], Cancellation)));

        Assert.Equal((Guid.Parse(mainImages.Id), null), await FolderOfAsync(letterboxing.Id));
        Assert.Equal(Chats.Key, (await FolderOfAsync(shared.Id)).Area);
        RpcException used = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => Done(library.DeleteAsync(letterboxing.Id, Cancellation))));
        Assert.Equal(StatusCode.FailedPrecondition, used.StatusCode);
        Assert.Equal(Guid.Parse(mainImages.Id), (await FolderOfAsync(letterboxing.Id)).Folder);
    }

    [Fact]
    public async Task AFilesPlaceIsWhereItIsNowAndNeverAFolderTheViewerCantOpen()
    {
        ClaimsPrincipal staff = Person(), member = Person(), overseer = Person();
        FolderInfo photos = await FoldersCallAsync(folders => folders.CreateAsync(staff, new CreateFolderRequest { Name = "Main Photos" }, Cancellation));
        FolderInfo year = await FoldersCallAsync(folders => folders.CreateAsync(staff, new CreateFolderRequest { Name = "2026", ParentId = photos.Id }, Cancellation));
        MediaRef banner = await UploadAsync(staff, Jpeg(seed: 43), "image/jpeg", fileName: "banner.jpg");
        MediaRef loose = await UploadAsync(staff, Jpeg(seed: 44), "image/jpeg", fileName: "loose.jpg");
        await FoldersCallAsync(folders => Done(folders.MoveAsync(new MoveRequest { ItemIds = [banner.Id], Into = photos.Id }, Cancellation)));
        Members.Add(member, "chat-11");
        MediaRef shared = (await SendAsync(member, Jpeg(seed: 45), new BeginUploadRequest
        {
            FileName = "camp.jpg", ContentType = "image/jpeg", Bytes = Jpeg(seed: 45).Length, Area = Chats.Key, AreaScopeId = "chat-11",
        })).Item!.Media!;
        await FoldersCallAsync(folders => Done(folders.FileAsync(Chats, "chat-11", "Youth group", [shared], Cancellation)));
        await FoldersCallAsync(folders => Done(folders.MoveAsync(new MoveRequest { ItemIds = [banner.Id], Into = year.Id }, Cancellation)));
        Access.Grant(overseer, MediaPermissions.SystemViewNode, null);

        Dictionary<string, string> seenByStaff = await PlacesAsync(staff, banner, loose, shared);
        Dictionary<string, string> seenByOverseer = await PlacesAsync(overseer, shared);

        Assert.Equal(("Main Photos/2026", "", ""), (seenByStaff[banner.Id], seenByStaff[loose.Id], seenByStaff[shared.Id]));
        Assert.Equal("Made by the app/Chats/Youth group", seenByOverseer[shared.Id]);
    }

    private async Task<Dictionary<string, string>> PlacesAsync(ClaimsPrincipal user, params MediaRef[] media) =>
        (await LibraryCallAsync(library => library.PlacesAsync(user, new MediaPlacesRequest { Ids = [.. media.Select(item => item.Id)] }, Cancellation)))
        .Items.ToDictionary(place => place.Id, place => place.Folder);

    private async Task<(Guid? Folder, string? Area)> FolderOfAsync(string mediaId)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        MediaDbContext db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        Guid id = Guid.Parse(mediaId);
        Guid? folder = await db.Items.Where(item => item.Id == id).Select(item => item.FolderId).SingleAsync(Cancellation);
        string? area = await db.Folders.Where(candidate => candidate.Id == folder).Select(candidate => candidate.SystemArea).SingleOrDefaultAsync(Cancellation);
        return (folder, area);
    }
}

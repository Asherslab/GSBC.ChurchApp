using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

public sealed class FolderRuleTests
{
    private static readonly Permission ReadFiles = new("ministries.files.read", ScopeKind.Ministry, "Read a ministry's files");
    private static readonly MediaSystemArea Chats = new("chats", "Chats", "Made by the app: files people share in group chats", ReadFiles);

    [Fact]
    public void AFolderCanNotGoInsideItselfOrAnythingInIt()
    {
        Guid sermons = Guid.CreateVersion7(), series = Guid.CreateVersion7(), events = Guid.CreateVersion7();
        Dictionary<Guid, Guid?> parents = new() { [sermons] = null, [series] = sermons, [events] = null };

        Assert.True(MediaFolders.IsWithin(parents, sermons, sermons));
        Assert.True(MediaFolders.IsWithin(parents, series, sermons));
        Assert.False(MediaFolders.IsWithin(parents, events, sermons));
    }

    [Fact]
    public void AFolderKeptByTheAppCanNotBeChosenRenamedMovedOrDeleted()
    {
        MediaFolder chats = new() { Id = Guid.CreateVersion7(), Name = "Chats", SystemArea = Chats.Key };
        MediaFolder events = new() { Id = Guid.CreateVersion7(), Name = "Events" };

        Assert.Equal(StatusCode.FailedPrecondition, Assert.Throws<RpcException>(() => MediaFolders.StaffFolder(chats)).StatusCode);
        Assert.Equal(StatusCode.FailedPrecondition, Assert.Throws<RpcException>(() => MediaFolders.StaffFolderToChange(chats)).StatusCode);
        Assert.Equal(events.Id, MediaFolders.StaffFolder(events));
        Assert.Same(events, MediaFolders.StaffFolderToChange(events));
    }

    [Fact]
    public void AFileTheAppKeepsInASystemFolderCanNotBeMovedOrDeletedButAStaffFileCan()
    {
        Guid chats = Guid.CreateVersion7();
        HashSet<Guid> systemFolders = [chats];
        MediaItem campPhoto = File(chats), poster = File(null);

        Assert.Equal(StatusCode.FailedPrecondition, Assert.Throws<RpcException>(() => MediaFolders.CheckMovable([poster, campPhoto], systemFolders)).StatusCode);
        Assert.Equal(StatusCode.FailedPrecondition, Assert.Throws<RpcException>(() => MediaLibrary.CheckDeletable(campPhoto, systemFolders, uses: 0)).StatusCode);
        MediaFolders.CheckMovable([poster], systemFolders);
        MediaLibrary.CheckDeletable(poster, systemFolders, uses: 0);
        Assert.Equal(StatusCode.FailedPrecondition, Assert.Throws<RpcException>(() => MediaLibrary.CheckDeletable(poster, systemFolders, uses: 1)).StatusCode);
    }

    [Fact]
    public void AFontFetchedFromGoogleThatNothingUsesCanBeDeletedButOtherFilesTheAppKeepsCanNot()
    {
        MediaFolder chats = new() { Id = Guid.CreateVersion7(), Name = "Chats", SystemArea = Chats.Key };
        MediaFolder fonts = new() { Id = Guid.CreateVersion7(), Name = FontsService.Area.Name, SystemArea = FontsService.Area.Key };
        HashSet<Guid> kept = MediaLibrary.KeptFolders([chats, fonts], new HashSet<string> { FontsService.Area.Key });
        MediaItem lobster = File(fonts.Id), campPhoto = File(chats.Id);

        MediaLibrary.CheckDeletable(lobster, kept, uses: 0);
        Assert.Equal(StatusCode.FailedPrecondition, Assert.Throws<RpcException>(() => MediaLibrary.CheckDeletable(lobster, kept, uses: 1)).StatusCode);
        Assert.Equal(StatusCode.FailedPrecondition, Assert.Throws<RpcException>(() => MediaLibrary.CheckDeletable(campPhoto, kept, uses: 0)).StatusCode);
        Assert.True(FontsService.Area.StaffDeleteUnused);
    }

    [Fact]
    public async Task OnlyAFoldersAudienceOrSomeoneOverseeingSystemFoldersSeesInside()
    {
        ClaimsPrincipal staff = Person(), member = Person(), pastor = Person();
        GrantedEvaluator access = new();
        access.Grant(member, ReadFiles.Node, "42");
        access.Grant(pastor, MediaPermissions.SystemViewNode, null);
        MediaFolder root = new() { Id = Guid.CreateVersion7(), Name = "Chats", SystemArea = Chats.Key };
        MediaFolder youth = new() { Id = Guid.CreateVersion7(), ParentId = root.Id, Name = "Youth group", SystemArea = Chats.Key, SystemScope = "42" };
        MediaFolder mens = new() { Id = Guid.CreateVersion7(), ParentId = root.Id, Name = "Men's group", SystemArea = Chats.Key, SystemScope = "7" };
        MediaFolder events = new() { Id = Guid.CreateVersion7(), Name = "Events" };
        MediaFolder[] folders = [root, youth, mens, events];
        Dictionary<string, MediaSystemArea> areas = new() { [Chats.Key] = Chats };

        Assert.Equal(new[] { root.Id, youth.Id, mens.Id }.Order(), (await HiddenAsync(staff)).Order());
        Assert.Equal(new[] { root.Id, mens.Id }.Order(), (await HiddenAsync(member)).Order());
        Assert.Empty(await HiddenAsync(pastor));

        Task<HashSet<Guid>> HiddenAsync(ClaimsPrincipal user) => MediaFolders.HiddenAsync(access, areas, user, folders, TestContext.Current.CancellationToken);
    }

    private static MediaItem File(Guid? folder) =>
        new() { Id = Guid.CreateVersion7(), Key = $"{new string('a', 64)}.jpg", Sha256 = new string('a', 64), ContentType = "image/jpeg", FolderId = folder };

    private static ClaimsPrincipal Person() =>
        new(new ClaimsIdentity([new Claim(Caller.UserIdClaim, Guid.CreateVersion7().ToString())], "test"));
}

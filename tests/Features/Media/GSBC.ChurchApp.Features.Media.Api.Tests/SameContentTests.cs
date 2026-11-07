using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Api.Data;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

public sealed class SameContentTests
{
    private const string Hash = "0a1b2c3d4e5f60718293a4b5c6d7e8f90a1b2c3d4e5f60718293a4b5c6d7e8f9";
    private static readonly DateTime Earlier = new(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Staff = Guid.CreateVersion7(), Leader = Guid.CreateVersion7(), UploaderOnly = Guid.CreateVersion7();
    private static readonly IReadOnlySet<Guid> NoSystemFolders = new HashSet<Guid>();

    [Fact]
    public void UploadingAFileTheLibraryAlreadyHasGivesBackTheOneItHas()
    {
        MediaItem first = Item(Staff), second = Item(Staff);

        Assert.Same(first, MediaLibrary.Choose([first, second], null).Kept);
    }

    [Fact]
    public void AMatchTheCallerCanNotSeeIsNeverRevealed()
    {
        MediaItem privateNotes = Item(Leader, audience: ("ministries.files.read", "42"));
        MediaItem poster = Item(Staff);
        Guid chats = Guid.CreateVersion7();
        MediaItem campPhoto = Item(Leader, folder: chats);
        MediaItem[] sameBytes = [privateNotes, poster, campPhoto];
        HashSet<Guid> systemFolders = [chats];

        Assert.Equal([poster], MediaLibrary.SameContent(sameBytes, null, null, Staff, seesLibrary: true, systemFolders));
        Assert.Empty(MediaLibrary.SameContent(sameBytes, null, null, UploaderOnly, seesLibrary: false, systemFolders));
        Assert.Equal([privateNotes], MediaLibrary.SameContent(sameBytes, "ministries.files.read", "42", Leader, seesLibrary: false, systemFolders));
        Assert.Equal([poster, campPhoto], MediaLibrary.SameContent(sameBytes, null, null, Staff, seesLibrary: true, NoSystemFolders));
    }

    [Fact]
    public void UploadingARemovedOriginalAgainOffersToPutItBackIntoTheOneNamed()
    {
        MediaItem removed = Item(Staff, removed: true), removedLater = Item(Staff, removed: true);

        SameContentChoice offered = MediaLibrary.Choose([removed, removedLater], null);
        SameContentChoice named = MediaLibrary.Choose([removed, removedLater], removedLater.Id.ToString());

        Assert.Equal((null, removed), (offered.Kept, offered.PutBack));
        Assert.Same(removedLater, named.PutBack);
    }

    [Fact]
    public void ARemovedOriginalIsOnlyOfferedBackToPeopleWhoCanSeeItsPicture()
    {
        MediaItem removed = Item(Staff, removed: true);
        List<MediaItem> seenByUploaderOnly = MediaLibrary.SameContent([removed], null, null, UploaderOnly, seesLibrary: false, NoSystemFolders);

        Assert.Empty(seenByUploaderOnly);
        Assert.Equal((null, null), (MediaLibrary.Choose(seenByUploaderOnly, null).Kept, MediaLibrary.Choose(seenByUploaderOnly, null).PutBack));
        Assert.Equal(StatusCode.NotFound, Assert.Throws<RpcException>(() => MediaLibrary.Choose(seenByUploaderOnly, removed.Id.ToString())).StatusCode);
    }

    private static MediaItem Item(Guid uploadedBy, (string Permission, string Scope)? audience = null, Guid? folder = null, bool removed = false) => new()
    {
        Id = Guid.CreateVersion7(),
        Key = $"{Hash}.jpg",
        Sha256 = Hash,
        ContentType = "image/jpeg",
        UploadedBy = uploadedBy,
        UploadedAt = Earlier,
        AudiencePermission = audience?.Permission,
        AudienceScopeId = audience?.Scope,
        FolderId = folder,
        OriginalRemovedAt = removed ? Earlier : null,
    };
}

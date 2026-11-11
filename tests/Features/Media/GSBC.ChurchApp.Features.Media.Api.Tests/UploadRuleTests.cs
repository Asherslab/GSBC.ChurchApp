using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

public sealed class UploadRuleTests
{
    private static readonly MediaAudience Group = new() { Permission = "ministries.files.read", ScopeId = "42" };

    [Fact]
    public void NobodyCanMakeAFilePrivateToPeopleTheyAreNotAmong()
    {
        BeginUploadRequest notes = new() { FileName = "notes.pdf", ContentType = "application/pdf", Bytes = 100, PrivateTo = Group };

        Assert.Equal(StatusCode.PermissionDenied, Refused(notes, knownAudience: true, amongAudience: false));
        Assert.Equal(StatusCode.InvalidArgument, Refused(notes, knownAudience: false, amongAudience: false));
        MediaLibrary.CheckUpload(notes, knownAudience: true, amongAudience: true, mayUploadFonts: false);
    }

    [Fact]
    public void OnlyPeopleTrustedWithFontLicencesCanUploadAFontFileAndItIsNeverPrivate()
    {
        BeginUploadRequest font = new() { FileName = "Brand.woff2", ContentType = "font/woff2", Bytes = 100 };

        Assert.Equal(StatusCode.PermissionDenied, Refused(font));
        MediaLibrary.CheckUpload(font, knownAudience: false, amongAudience: false, mayUploadFonts: true);
        font.PrivateTo = Group;
        Assert.Equal(StatusCode.InvalidArgument, Refused(font, knownAudience: true, amongAudience: true, mayUploadFonts: true));
    }

    [Fact]
    public void AnAreaTakesOnlyItsOwnFileTypesEachUpToItsSizeAndElsewhereOnlyTheLibrarysTypes()
    {
        MediaSystemArea videos = new("clips", "Clips", "Made by the app", new Permission("clips.read", ScopeKind.Global, "Read clips"),
            Files: [new AreaFileType(MediaTypes.Mp4, 100), new AreaFileType("image/png", 10)]);

        MediaLibrary.CheckAreaFile(videos, MediaTypes.Mp4, 100);
        Assert.Throws<RpcException>(() => MediaLibrary.CheckAreaFile(videos, MediaTypes.Mp4, 101));
        Assert.Throws<RpcException>(() => MediaLibrary.CheckAreaFile(videos, "application/pdf", 1));
        Assert.Throws<RpcException>(() => MediaLibrary.CheckAreaFile(null, MediaTypes.Mp4, 1));
        MediaLibrary.CheckAreaFile(null, "application/pdf", long.MaxValue);
    }

    private static StatusCode Refused(BeginUploadRequest request, bool knownAudience = false, bool amongAudience = false, bool mayUploadFonts = false) =>
        Assert.Throws<RpcException>(() => MediaLibrary.CheckUpload(request, knownAudience, amongAudience, mayUploadFonts)).StatusCode;
}

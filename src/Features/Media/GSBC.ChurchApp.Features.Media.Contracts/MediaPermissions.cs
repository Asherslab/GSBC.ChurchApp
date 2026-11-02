using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Media.Contracts;

public static class MediaPermissions
{
    public const string ViewNode = "media.view";
    public const string UploadNode = "media.upload";
    public const string DeleteNode = "media.delete";
    public const string UploadFontsNode = "media.fonts.upload";

    public static readonly Permission View =
        new(ViewNode, ScopeKind.Global, "See the media library: every uploaded image and file");

    public static readonly Permission Upload =
        new(UploadNode, ScopeKind.Global, "Upload images and files");

    public static readonly Permission Delete =
        new(DeleteNode, ScopeKind.Global, "Delete images and files nothing uses any more");

    public static readonly Permission UploadFonts =
        new(UploadFontsNode, ScopeKind.Global, "Upload font files: only fonts the church is licensed to use in an app. Fonts from Google Fonts need only Upload");

    public static readonly IReadOnlyList<Permission> All = [View, Upload, Delete, UploadFonts];
}

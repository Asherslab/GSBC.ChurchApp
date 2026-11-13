using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Media;

public static class PostMedia
{
    public const long LargestPicture = 25L * 1024 * 1024;
    public const long LargestFile = 100L * 1024 * 1024;
    public const long LargestWithLargeUploads = 2L * 1024 * 1024 * 1024;
    public const string LargeUploadsNode = "media.uploads.large";

    public static readonly Permission LargeUploads = new(LargeUploadsNode, ScopeKind.Global,
        "Add video and audio up to 2 GB to news posts, where everyone else can add up to 100 MB", area: "Media");

    public static readonly IReadOnlyList<AreaFileType> Types =
    [
        new("image/jpeg", LargestPicture), new("image/png", LargestPicture), new("image/webp", LargestPicture), new("image/gif", LargestPicture),
        new("application/pdf", LargestFile),
        Large("video/mp4"), Large("video/quicktime"), Large("audio/mpeg"), Large("audio/mp4"), Large("audio/x-m4a"),
    ];

    private static AreaFileType Large(string contentType) => new(contentType, LargestFile, LargeUploads, LargestWithLargeUploads);
}

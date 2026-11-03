using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Features.Media.Contracts;

public static class MediaDestinations
{
    public static readonly DestinationArea Files = new("files", "Files", "file", 30);

    public static readonly DestinationKind File = new("media.file", Files, "File", "file", "files", OpensOutside: true, Feeds: [MediaFeeds.Library]);

    public static readonly DestinationSet All = new([], [File]);
}

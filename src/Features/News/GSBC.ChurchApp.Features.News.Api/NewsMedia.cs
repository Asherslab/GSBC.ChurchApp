using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;

namespace GSBC.ChurchApp.Features.News.Api;

internal static class NewsMedia
{
    public static readonly MediaSystemArea Area = new(NewsAreas.Media, "Church news",
        "Made by the app: the pictures, video, audio and files in church news posts. One nothing uses any more can be deleted.",
        NewsPermissions.Write, StaffDeleteUnused: true, Uploaders: NewsPermissions.Write, Files: PostMedia.Types);
}

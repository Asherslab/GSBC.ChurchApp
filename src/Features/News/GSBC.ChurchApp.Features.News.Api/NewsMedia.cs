using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;

namespace GSBC.ChurchApp.Features.News.Api;

internal static class NewsMedia
{
    public static readonly MediaSystemArea Area = new(NewsAreas.Media, "Church news",
        "Made by the app: the pictures, video, audio and files uploaded into church news posts. You can delete any that no post uses any more.",
        NewsPermissions.Write, StaffDeleteUnused: true, Uploaders: NewsPermissions.Write, Files: PostMedia.Types);
}

using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal static class SermonMedia
{
    public static readonly MediaSystemArea Pictures = new("sermons", "Sermon artwork",
        "Pictures of sermons, series and speakers that the app keeps itself, such as those brought across from the old website. Each picture is kept once, however many sermons use it, and Media makes the sizes phones need.",
        SermonsPermissions.Edit);

    public static readonly MediaSystemArea Audio = new("sermons-audio", "Sermon audio",
        "The sermons' audio in the church's own storage: what the AV team uploads and what was brought across from the old website. Each recording is kept once.",
        SermonsPermissions.Edit);

    public static string SermonOwner(Guid id) => $"sermons/sermon/{id}";

    public static string AudioOwner(Guid id) => $"sermons/sermon/{id}/audio";

    public static string SeriesOwner(Guid id) => $"sermons/series/{id}";

    public static string SpeakerOwner(Guid id) => $"sermons/speakers/{id}";
}

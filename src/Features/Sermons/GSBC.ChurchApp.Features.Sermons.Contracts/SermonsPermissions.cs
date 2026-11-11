using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Sermons.Contracts;

public static class SermonsPermissions
{
    public const string EditNode = "sermons.edit";
    public const string UploadNode = "sermons.upload";
    public const string EditorRole = "sermons-editor";
    public const string UploaderRole = "sermons-uploader";
    public const string HearNewNode = "sermons.new.receive";
    public const string HearSeriesNode = "sermons.series.receive";
    public const string FollowerRole = "sermons-follower";
    public const string SeriesFollowerRole = "sermons-series-follower";

    public static readonly Permission HearNew =
        new(HearNewNode, ScopeKind.Global, "Hear when a new sermon is out", area: "Sermons");

    public static readonly Permission HearSeries =
        new(HearSeriesNode, ScopeKind.SermonSeries, "Hear when a series has a new sermon", area: "Sermons");

    public static readonly Permission Edit =
        new(EditNode, ScopeKind.Global, "Correct sermons and look after series and speakers", area: "Sermons");

    public static readonly Permission Upload =
        new(UploadNode, ScopeKind.Global, "Add sermons by uploading their audio", area: "Sermons", opensAdminArea: true);

    public static readonly IReadOnlyList<Permission> All = [Edit, Upload, HearNew, HearSeries];
}

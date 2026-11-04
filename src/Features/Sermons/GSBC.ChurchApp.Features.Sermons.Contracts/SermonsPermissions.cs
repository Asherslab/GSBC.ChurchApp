using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Sermons.Contracts;

public static class SermonsPermissions
{
    public const string EditNode = "sermons.edit";
    public const string SourceNode = "sermons.source";
    public const string UploadNode = "sermons.upload";
    public const string EditorRole = "sermons-editor";
    public const string UploaderRole = "sermons-uploader";

    public static readonly Permission Edit =
        new(EditNode, ScopeKind.Global, "Correct sermons and look after series and speakers", area: "Sermons");

    public static readonly Permission Upload =
        new(UploadNode, ScopeKind.Global, "Add sermons by uploading their audio", area: "Sermons");

    public static readonly Permission Source =
        new(SourceNode, ScopeKind.Global, "Choose where new sermons come from", area: "Sermons");

    public static readonly IReadOnlyList<Permission> All = [Edit, Upload, Source];
}

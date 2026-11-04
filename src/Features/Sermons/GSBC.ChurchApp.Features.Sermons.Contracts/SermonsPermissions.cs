using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Sermons.Contracts;

public static class SermonsPermissions
{
    public const string EditNode = "sermons.edit";
    public const string SourceNode = "sermons.source";
    public const string EditorRole = "sermons-editor";

    public static readonly Permission Edit =
        new(EditNode, ScopeKind.Global, "Correct sermons, look after series and speakers, and add sermons", area: "Sermons");

    public static readonly Permission Source =
        new(SourceNode, ScopeKind.Global, "Choose where new sermons come from", area: "Sermons");

    public static readonly IReadOnlyList<Permission> All = [Edit, Source];
}

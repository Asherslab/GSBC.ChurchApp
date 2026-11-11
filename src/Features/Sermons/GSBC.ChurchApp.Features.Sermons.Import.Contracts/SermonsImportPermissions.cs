using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Sermons.Import.Contracts;

public static class SermonsImportPermissions
{
    public const string ImportNode = "sermons.import";

    public static readonly Permission Import =
        new(ImportNode, ScopeKind.Global, "Import every sermon from the old website", area: "Sermons", opensAdminArea: true);

    public static readonly IReadOnlyList<Permission> All = [Import];
}

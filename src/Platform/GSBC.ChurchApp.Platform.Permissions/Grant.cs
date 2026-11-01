namespace GSBC.ChurchApp.Platform.Permissions;

public readonly record struct Grant(PermissionPattern Pattern, Scope Scope)
{
    public bool AppliesTo(Permission permission, string? objectId) =>
        Scope.IsGlobal || (Scope.Kind == permission.ScopeKind && Scope.ObjectId == objectId);
}

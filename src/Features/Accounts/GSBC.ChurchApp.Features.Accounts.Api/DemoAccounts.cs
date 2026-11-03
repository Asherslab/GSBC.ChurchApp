using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal sealed class DemoAccounts(AccountsDbContext db, IRoleAssignments roles, IAccessEvaluator access, PermissionCatalog catalog)
{
    public const string RoleKey = "app-reviewer";

    public static readonly SystemRole Role = new(RoleKey, "App reviewer",
        "Marks a demo account that Apple's app reviewers sign in to with a sign-in code. It lets them do nothing more.",
        ScopeKind.Global, []);

    public async Task<IReadOnlyList<Guid>> AllAsync(CancellationToken cancellationToken) =>
        (await roles.HoldersAsync(RoleKey, null, cancellationToken)).Select(holder => holder.UserId).ToList();

    public ValueTask MakeAsync(Guid userId, CancellationToken cancellationToken) =>
        roles.GiveAsFeatureAsync(userId, RoleKey, null, null, cancellationToken);

    public async Task<bool> IsDemoAsync(Guid userId, CancellationToken cancellationToken) =>
        (await AllAsync(cancellationToken)).Contains(userId);

    public async Task<string?> WhyCodesCannotSignInAsync(AccountUser user, CancellationToken cancellationToken)
    {
        if (!await IsDemoAsync(user.Id, cancellationToken))
            return "This isn't a demo account.";
        if (user.Disabled)
            return "This demo account is turned off.";
        if (await db.Set<IdentityUserPasskey<Guid>>().AnyAsync(passkey => passkey.UserId == user.Id, cancellationToken))
            return "This account has a passkey, so it belongs to a real person. Codes can't sign in to it.";
        if (await HoldsSensitivePermissionAsync(user.Id, cancellationToken))
            return "This account holds a role that needs extra care, so codes can't sign in to it. Take that role away first.";
        return null;
    }

    public async Task<bool> HoldsSensitivePermissionAsync(Guid userId, CancellationToken cancellationToken)
    {
        GrantSet grants = await access.GrantsForAsync(userId, cancellationToken);
        List<string> sensitive = catalog.ByNode.Values.Where(permission => permission.IsSensitive).Select(permission => permission.Node).ToList();
        return grants.Grants.Any(grant => !grant.Pattern.IsDeny && sensitive.Any(grant.Pattern.Matches));
    }
}

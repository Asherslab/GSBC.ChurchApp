using System.Security.Claims;
using GSBC.ChurchApp.Features.Accounts.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Accounts.Gateway;

internal sealed class AccountClaimsPrincipalFactory(UserManager<AccountUser> users, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<AccountUser>(users, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AccountUser user)
    {
        ClaimsIdentity identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(AccountClaims.Name, user.DisplayName));
        return identity;
    }
}

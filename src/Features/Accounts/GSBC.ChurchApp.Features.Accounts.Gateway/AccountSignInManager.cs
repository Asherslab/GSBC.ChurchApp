using GSBC.ChurchApp.Features.Accounts.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Accounts.Gateway;

internal sealed class AccountSignInManager(
    UserManager<AccountUser> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<AccountUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<AccountUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<AccountUser> confirmation)
    : SignInManager<AccountUser>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    public override async Task<bool> CanSignInAsync(AccountUser user) => !user.Disabled && await base.CanSignInAsync(user);
}

using Microsoft.AspNetCore.Identity;

namespace GSBC.ChurchApp.Features.Accounts.Data;

public sealed class AccountUserValidator(IdentityErrorDescriber? errors = null) : UserValidator<AccountUser>(errors)
{
    public override async Task<IdentityResult> ValidateAsync(UserManager<AccountUser> manager, AccountUser user)
    {
        IdentityResult result = await base.ValidateAsync(manager, user);
        if (result.Succeeded || user is not { AddedByFamily: true, Email: null })
            return result;

        IdentityError[] others = result.Errors.Where(error => error.Code != nameof(IdentityErrorDescriber.InvalidEmail)).ToArray();
        return others.Length == 0 ? IdentityResult.Success : IdentityResult.Failed(others);
    }
}

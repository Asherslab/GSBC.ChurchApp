using Microsoft.AspNetCore.Identity;

namespace GSBC.ChurchApp.Features.Accounts.Data;

public sealed class AccountUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Disabled { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? ForgottenAt { get; set; }
}

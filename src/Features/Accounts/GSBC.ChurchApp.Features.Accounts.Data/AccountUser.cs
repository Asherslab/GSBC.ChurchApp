using Microsoft.AspNetCore.Identity;

namespace GSBC.ChurchApp.Features.Accounts.Data;

public sealed class AccountUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }
    public DateTime CreatedAt { get; init; }
    public bool Disabled { get; set; }
    public bool AddedByFamily { get; set; }
    public bool InvitationHeld { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? ForgottenAt { get; set; }
    public List<string> PinnedPages { get; set; } = [];
}

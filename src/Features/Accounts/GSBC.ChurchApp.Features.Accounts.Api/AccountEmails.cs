using GSBC.ChurchApp.Platform.Api.Email;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal static class AccountEmails
{
    public static EmailMessage Confirm(string to, string name, string code) => new(to,
        $"Your Good Shepherd confirmation code: {code}",
        $"Hi {name},\n\nYour code to confirm this email address is {code}. It works for 24 hours.\n\nIf you didn't create a Good Shepherd account, you can ignore this email.");

    public static EmailMessage Invite(string to, string name, string link) => new(to,
        "You're invited to the Good Shepherd app",
        $"Hi {name},\n\nYou've been invited to the Good Shepherd app. Open this link on your phone or computer to set up your sign-in (it works for 7 days):\n\n{link}");

    public static EmailMessage Recover(string to, string name, string link) => new(to,
        "Get back into your Good Shepherd account",
        $"Hi {name},\n\nOpen this link to add a new passkey and get back into your account (it works for 2 hours):\n\n{link}\n\nIf you didn't ask for this, you can ignore it; nothing changes until the link is used.");
}

using System.Security.Claims;
using System.Text.Encodings.Web;
using GSBC.ChurchApp.Features.Accounts.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Accounts.Gateway;

internal sealed class AppSessionHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IServiceScopeFactory scopes, TimeProvider time)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "AppSession";
    private static readonly TimeSpan LastSeenResolution = TimeSpan.FromMinutes(5);

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers[AppSessions.Header].FirstOrDefault() is not { Length: > 0 } secret)
            return AuthenticateResult.NoResult();

        string hash = AppSessions.HashOf(secret);
        DateTime now = time.GetUtcNow().UtcDateTime;
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        AccountsDbContext db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
        var found = await db.Sessions
            .Where(session => session.SecretHash == hash && session.Client == SessionClient.App && session.ExpiresAt > now)
            .Where(ReviewCodes.SessionIsCurrent(db, now))
            .Join(db.Users, session => session.UserId, user => user.Id, (session, user) => new { Session = session, user.DisplayName, user.Disabled })
            .SingleOrDefaultAsync(Context.RequestAborted);
        if (found is null || found.Disabled)
            return AuthenticateResult.Fail("This phone's session has ended. Sign in again.");

        if (now - found.Session.LastSeenAt > LastSeenResolution)
        {
            found.Session.LastSeenAt = now;
            found.Session.ExpiresAt = now + AppSessions.Lifetime;
            await db.SaveChangesAsync(Context.RequestAborted);
        }

        ClaimsIdentity identity = new(
            [
                new Claim(ClaimTypes.NameIdentifier, found.Session.UserId.ToString()),
                new Claim(AccountClaims.SessionId, found.Session.Id.ToString()),
                new Claim(AccountClaims.Name, found.DisplayName),
            ],
            SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}

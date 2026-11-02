using System.Collections.Concurrent;
using System.Security.Claims;
using GSBC.ChurchApp.Features.Accounts.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Gateway;

internal sealed class DatabaseTicketStore(IServiceScopeFactory scopes, TimeProvider time) : ITicketStore
{
    public static readonly TimeSpan BrowserLifetime = TimeSpan.FromDays(14);
    private static readonly TimeSpan LastSeenResolution = TimeSpan.FromMinutes(5);
    private const int MaximumDeviceNameLength = 200;
    private readonly ConcurrentDictionary<string, Task<AuthenticationTicket?>> _inFlight = new();

    public Task<string> StoreAsync(AuthenticationTicket ticket) => StoreAsync(ticket, null!, CancellationToken.None);

    public async Task<string> StoreAsync(AuthenticationTicket ticket, HttpContext httpContext, CancellationToken cancellationToken)
    {
        Guid sessionId = Guid.CreateVersion7();
        (ticket.Principal.Identity as ClaimsIdentity)?.AddClaim(new Claim(AccountClaims.SessionId, sessionId.ToString()));
        DateTime now = Now;

        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        AccountsDbContext db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
        db.Sessions.Add(new AccountSession
        {
            Id = sessionId,
            UserId = Guid.Parse(ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier)!),
            Client = SessionClient.Browser,
            DeviceName = DeviceName(httpContext),
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = ticket.Properties.ExpiresUtc?.UtcDateTime ?? now + BrowserLifetime,
            Ticket = TicketSerializer.Default.Serialize(ticket),
        });
        await db.SaveChangesAsync(cancellationToken);
        return sessionId.ToString();
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key) => RetrieveAsync(key, null!, CancellationToken.None);

    public Task<AuthenticationTicket?> RetrieveAsync(string key, HttpContext httpContext, CancellationToken cancellationToken) =>
        _inFlight.GetOrAdd(key, LookUpOnceAsync);

    private async Task<AuthenticationTicket?> LookUpOnceAsync(string key)
    {
        await Task.Yield();
        try
        {
            return await LookUpAsync(key);
        }
        finally
        {
            _inFlight.TryRemove(key, out _);
        }
    }

    private async Task<AuthenticationTicket?> LookUpAsync(string key)
    {
        if (!Guid.TryParse(key, out Guid id))
            return null;

        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        AccountsDbContext db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
        DateTime now = Now;
        var found = await db.Sessions
            .Where(session => session.Id == id && session.ExpiresAt > now)
            .Join(db.Users, session => session.UserId, user => user.Id, (session, user) => new { Session = session, user.Disabled })
            .SingleOrDefaultAsync(CancellationToken.None);
        if (found is null || found.Disabled)
            return null;

        if (now - found.Session.LastSeenAt > LastSeenResolution)
        {
            found.Session.LastSeenAt = now;
            await db.SaveChangesAsync(CancellationToken.None);
        }

        return TicketSerializer.Default.Deserialize(found.Session.Ticket);
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket) => RenewAsync(key, ticket, null!, CancellationToken.None);

    public async Task RenewAsync(string key, AuthenticationTicket ticket, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(key, out Guid id))
            return;

        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        AccountsDbContext db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
        byte[] serialized = TicketSerializer.Default.Serialize(ticket);
        DateTime expires = ticket.Properties.ExpiresUtc?.UtcDateTime ?? Now + BrowserLifetime;
        await db.Sessions.Where(session => session.Id == id).ExecuteUpdateAsync(
            update => update.SetProperty(session => session.Ticket, serialized).SetProperty(session => session.ExpiresAt, expires),
            cancellationToken);
    }

    public Task RemoveAsync(string key) => RemoveAsync(key, null!, CancellationToken.None);

    public async Task RemoveAsync(string key, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(key, out Guid id))
            return;

        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AccountsDbContext>().Sessions
            .Where(session => session.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private static string? DeviceName(HttpContext? httpContext)
    {
        string? agent = httpContext?.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(agent) ? null : agent[..Math.Min(agent.Length, MaximumDeviceNameLength)];
    }
}

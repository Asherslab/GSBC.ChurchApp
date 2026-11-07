using System.Security.Cryptography;
using GSBC.ChurchApp.Features.Events.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed record LinkHolder(Guid? UserId, Guid? InstallId);

internal sealed class CalendarLinkSecrets(EventsDbContext db, TimeProvider time)
{
    private const int SecretBytes = 24;
    private const int SecretLength = SecretBytes / 3 * 4;

    public async Task<string> OfAsync(LinkHolder holder, CancellationToken cancellationToken)
    {
        if (holder.UserId is { } person && holder.InstallId is { } install)
            await ClaimForPersonAsync(install, person, cancellationToken);
        CalendarLinkRecord? link = await Owned(holder).OrderBy(found => found.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        return link?.Secret ?? await NewAsync(holder, cancellationToken);
    }

    public async Task<string> ResetAsync(LinkHolder holder, CancellationToken cancellationToken)
    {
        if (holder.UserId is { } person && holder.InstallId is { } install)
            await ClaimForPersonAsync(install, person, cancellationToken);
        await Owned(holder).ExecuteDeleteAsync(cancellationToken);
        return await NewAsync(holder, cancellationToken);
    }

    public async Task ClaimForPersonAsync(Guid installId, Guid userId, CancellationToken cancellationToken) =>
        await db.CalendarLinks.Where(link => link.InstallId == installId && link.UserId == null)
            .ExecuteUpdateAsync(set => set.SetProperty(link => link.UserId, userId).SetProperty(link => link.InstallId, (Guid?)null), cancellationToken);

    public async Task<LinkHolder?> HolderAsync(string secret, CancellationToken cancellationToken)
    {
        if (secret.Length != SecretLength)
            return null;
        CalendarLinkRecord? link = await db.CalendarLinks.AsNoTracking().SingleOrDefaultAsync(found => found.Secret == secret, cancellationToken);
        return link is null ? null : new LinkHolder(link.UserId, link.UserId is null ? link.InstallId : null);
    }

    private IQueryable<CalendarLinkRecord> Owned(LinkHolder holder) => holder.UserId is { } person
        ? db.CalendarLinks.Where(link => link.UserId == person)
        : db.CalendarLinks.Where(link => link.UserId == null && link.InstallId == holder.InstallId);

    private async Task<string> NewAsync(LinkHolder holder, CancellationToken cancellationToken)
    {
        CalendarLinkRecord link = new()
        {
            Secret = System.Buffers.Text.Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(SecretBytes)),
            UserId = holder.UserId,
            InstallId = holder.UserId is null ? holder.InstallId : null,
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };
        db.CalendarLinks.Add(link);
        await db.SaveChangesAsync(cancellationToken);
        return link.Secret;
    }
}

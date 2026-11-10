using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.People.Api;

internal sealed class PeopleBlueCards(PeopleDbContext db) : IBlueCards
{
    public async ValueTask<IReadOnlySet<Guid>> HoldingCurrentAsync(IReadOnlyCollection<Guid> people, DateOnly today, CancellationToken cancellationToken)
    {
        List<SafeguardingEntry> entries = await db.Safeguarding.AsNoTracking()
            .Where(entry => people.Contains(entry.UserId))
            .ToListAsync(cancellationToken);
        return entries.Where(entry => Current(entry, today)).Select(entry => entry.UserId).ToHashSet();
    }

    internal static bool Current(SafeguardingEntry entry, DateOnly today) =>
        entry.CardStatus is { } status && BlueCards.Holds(status) && (entry.CardExpires is not { } expires || expires >= today);
}

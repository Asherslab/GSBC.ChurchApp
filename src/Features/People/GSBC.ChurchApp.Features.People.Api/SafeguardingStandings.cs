using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.People.Api;

internal sealed class SafeguardingStandings(PeopleDbContext db) : ISafeguardingStandings
{
    public async ValueTask<IReadOnlyDictionary<Guid, SafeguardingStanding>> OfAsync(IReadOnlyCollection<Guid> people, DateOnly on, CancellationToken cancellationToken)
    {
        List<Guid> wanted = people.Distinct().ToList();
        List<SafeguardingEntry> entries = await db.Safeguarding.AsNoTracking().Where(entry => wanted.Contains(entry.UserId)).ToListAsync(cancellationToken);
        return entries.ToDictionary(entry => entry.UserId, entry => Standing(entry, on));
    }

    internal static SafeguardingStanding Standing(SafeguardingEntry entry, DateOnly on) =>
        new(entry.UserId,
            HoldsBlueCard: entry.CardStatus is { } status && BlueCards.Holds(status) && (entry.CardExpires is null || entry.CardExpires >= on),
            TrainingCurrent: entry.TrainingDone is not null && (entry.TrainingDue is null || entry.TrainingDue >= on)
                && (string.IsNullOrEmpty(entry.TrainingProgress) || SafeMinistryTraining.Finished(entry.TrainingProgress)));
}

using GSBC.ChurchApp.Features.Rosters.Api.Data;
using GSBC.ChurchApp.Features.Rosters.Contracts;
using GSBC.ChurchApp.Platform.Api.Ministries;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Rosters.Api;

internal sealed class RosterViews(
    RostersDbContext db, IMinistryFacts ministries, IFamilies families, IPeopleIndex people, ISafeguardingStandings safeguarding, TimeProvider time)
{
    public (DateTime From, DateTime To) Window()
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        return (now.AddDays(-RostersFeeds.DaysBack), now.AddMonths(RostersFeeds.MonthsAhead));
    }

    public async Task<RosterBook?> LoadAsync(Guid ministryId, CancellationToken cancellationToken, DateTime? from = null, DateTime? to = null)
    {
        if (await ministries.FindAsync(ministryId, cancellationToken) is not { } ministry)
            return null;

        (DateTime windowFrom, DateTime windowTo) = Window();
        DateTime start = from ?? windowFrom;
        DateTime end = to ?? windowTo;
        SettingsRecord? settings = await db.Settings.AsNoTracking().SingleOrDefaultAsync(each => each.MinistryId == ministryId, cancellationToken);
        List<PositionRecord> positions = await db.Positions.AsNoTracking().Where(position => position.MinistryId == ministryId).ToListAsync(cancellationToken);
        List<Guid> positionIds = positions.Select(position => position.Id).ToList();
        List<TrainedRecord> trained = await db.Trained.AsNoTracking().Where(each => positionIds.Contains(each.PositionId)).ToListAsync(cancellationToken);
        List<DateRecord> dates = await db.Dates.AsNoTracking()
            .Where(date => date.MinistryId == ministryId && date.EndsAt >= start && date.StartsAt <= end).ToListAsync(cancellationToken);
        List<Guid> dateIds = dates.Select(date => date.Id).ToList();
        List<AssignmentRecord> assignments = await db.Assignments.AsNoTracking().Where(assignment => dateIds.Contains(assignment.DateId)).ToListAsync(cancellationToken);
        List<Guid> assignmentIds = assignments.Select(assignment => assignment.Id).ToList();
        List<SwapRecord> swaps = await db.Swaps.AsNoTracking().Where(swap => assignmentIds.Contains(swap.AssignmentId)).ToListAsync(cancellationToken);
        IReadOnlyList<MinistryMember> team = await ministries.TeamAsync(ministryId, cancellationToken);

        List<Guid> involved = team.Select(member => member.PersonId)
            .Concat(assignments.Select(assignment => assignment.PersonId).OfType<Guid>())
            .Distinct().ToList();
        IReadOnlyDictionary<Guid, FamilyFacts> facts = await families.FactsAsync(involved, cancellationToken);
        List<Guid> named = involved
            .Concat(facts.Values.SelectMany(fact => fact.Parents))
            .Concat(assignments.Select(assignment => assignment.RepliedBy).OfType<Guid>())
            .Concat(swaps.SelectMany(swap => new[] { swap.AskedTo, swap.OfferedBy }).OfType<Guid>())
            .Distinct().ToList();
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(named, cancellationToken);
        DateOnly today = ChurchTime.Today(time);
        IReadOnlyDictionary<Guid, SafeguardingStanding> standings = await safeguarding.OfAsync(involved, today, cancellationToken);
        List<AwayRecord> away = await db.Away.AsNoTracking().Where(span => involved.Contains(span.PersonId) && (span.To == null || span.To >= today.AddDays(-RostersFeeds.DaysBack)))
            .ToListAsync(cancellationToken);

        return new RosterBook(ministry, settings, positions, trained, dates, assignments, swaps, team, facts, names, standings, away,
            time.GetUtcNow().UtcDateTime);
    }
}

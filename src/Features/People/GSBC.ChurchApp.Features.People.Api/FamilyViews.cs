using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Api.Email;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.People.Api;

internal sealed class FamilyViews(PeopleDbContext db, IPeopleAccounts accounts, IAccountEmails emails)
{
    public async Task<PersonFamily> FamilyAsync(Guid personId, bool safeguardingShown, CancellationToken cancellationToken)
    {
        List<FamilyLink> links = await db.Links.AsNoTracking().Where(link => link.ChildId == personId || link.ParentId == personId).ToListAsync(cancellationToken);
        List<LinkAskRecord> asks = await db.Asks.AsNoTracking().Where(ask => ask.ChildId == personId || ask.AskedBy == personId)
            .OrderBy(ask => ask.AskedAt).ToListAsync(cancellationToken);
        List<Guid> ids = [personId, .. links.SelectMany(link => new[] { link.ChildId, link.ParentId }), .. links.Select(link => link.LinkedBy).OfType<Guid>()];
        Dictionary<Guid, PersonRecord> records = await RecordsAsync(ids, cancellationToken);
        IReadOnlyDictionary<Guid, PersonAccount> people = await accounts.GetAsync(ids.Distinct().ToList(), cancellationToken);
        List<string> theirEmails = (await emails.EmailsOfAsync([personId], cancellationToken)).Select(email => email.Trim().ToUpperInvariant()).ToList();
        List<LinkAskRecord> askedOfThem = await db.Asks.AsNoTracking().Where(ask => theirEmails.Contains(ask.ParentEmail)).OrderBy(ask => ask.AskedAt).ToListAsync(cancellationToken);
        SafeguardingEntry? safeguarding = safeguardingShown
            ? await db.Safeguarding.AsNoTracking().SingleOrDefaultAsync(entry => entry.UserId == personId, cancellationToken)
            : null;

        PersonAccount? account = people.GetValueOrDefault(personId);
        PersonRecord? record = records.GetValueOrDefault(personId);
        return new PersonFamily
        {
            Id = personId.ToString(),
            Name = account?.Name ?? "",
            BirthDate = WireDates.ToWire(record?.BirthDate),
            SaysAdult = record?.SaysAdult ?? false,
            CanSignIn = account?.CanSignIn ?? false,
            AddedByFamily = account?.AddedByFamily ?? false,
            Parents = Cards(links.Where(link => link.ChildId == personId), link => link.ParentId, people, records),
            Children = Cards(links.Where(link => link.ParentId == personId), link => link.ChildId, people, records),
            Asks = await AsksAsync(asks, cancellationToken),
            AskedOfThem = await AsksAsync(askedOfThem, cancellationToken),
            SafeguardingShown = safeguardingShown,
            Safeguarding = safeguarding is null ? null : await RecordAsync(safeguarding, cancellationToken),
        };
    }

    public async Task<List<LinkAsk>> AsksAsync(IReadOnlyCollection<LinkAskRecord> asks, CancellationToken cancellationToken)
    {
        List<Guid> ids = asks.SelectMany(ask => new[] { ask.ChildId, ask.AskedBy }).Distinct().ToList();
        IReadOnlyDictionary<Guid, PersonAccount> people = await accounts.GetAsync(ids, cancellationToken);
        Dictionary<Guid, PersonRecord> records = await RecordsAsync(ids, cancellationToken);
        return asks.Select(ask => new LinkAsk
        {
            Id = ask.Id.ToString(),
            ChildId = ask.ChildId.ToString(),
            ChildName = people.GetValueOrDefault(ask.ChildId)?.Name ?? "",
            ChildBirthDate = WireDates.ToWire(records.GetValueOrDefault(ask.ChildId)?.BirthDate),
            ParentEmail = ask.ParentEmail.ToLowerInvariant(),
            AskedByName = people.GetValueOrDefault(ask.AskedBy)?.Name ?? "",
            AskedAt = ask.AskedAt,
            AskedByChild = ask.AskedBy == ask.ChildId,
        }).ToList();
    }

    public async Task<IReadOnlyList<FlaggedPerson>> FlaggedAsync(DateOnly today, CancellationToken cancellationToken)
    {
        List<SafeguardingEntry> entries = await db.Safeguarding.AsNoTracking().ToListAsync(cancellationToken);
        DateOnly adultsBornBefore = today.AddYears(-Ages.Adult);
        List<PersonRecord> young = await db.People.AsNoTracking()
            .Where(person => person.BirthDate > adultsBornBefore && !db.Links.Any(link => link.ChildId == person.UserId))
            .ToListAsync(cancellationToken);
        Dictionary<Guid, DateTime> asked = await db.Asks.AsNoTracking()
            .GroupBy(ask => ask.ChildId)
            .Select(group => new { group.Key, First = group.Min(ask => ask.AskedAt) })
            .ToDictionaryAsync(group => group.Key, group => group.First, cancellationToken);

        List<Guid> ids = entries.Select(entry => entry.UserId).Concat(young.Select(person => person.UserId)).Distinct().ToList();
        IReadOnlyDictionary<Guid, PersonAccount> people = await accounts.GetAsync(ids, cancellationToken);
        bool Live(Guid id) => people.TryGetValue(id, out PersonAccount? account) && !account.Deleted;

        IEnumerable<(Guid Person, SafeguardingFlag Flag)> flags = entries.Where(entry => Live(entry.UserId))
            .SelectMany(entry => SafeguardingFlags.For(Record(entry), today).Select(flag => (entry.UserId, flag)))
            .Concat(young.Where(person => Live(person.UserId))
                .Select(person => (person.UserId, SafeguardingFlags.NoParent(asked.TryGetValue(person.UserId, out DateTime at) ? DateOnly.FromDateTime(at) : null))));

        return flags
            .OrderBy(flagged => FlagOrder(flagged.Flag.Kind))
            .ThenBy(flagged => flagged.Flag.When ?? DateOnly.MaxValue)
            .ThenBy(flagged => people[flagged.Person].Name)
            .Select(flagged => new FlaggedPerson
            {
                PersonId = flagged.Person.ToString(),
                Name = people[flagged.Person].Name,
                Kind = flagged.Flag.Kind,
                When = WireDates.ToWire(flagged.Flag.When),
                Words = flagged.Flag.Words,
            })
            .ToList();
    }

    private static int FlagOrder(string kind) => kind switch
    {
        FlagKinds.RunOut => 0,
        FlagKinds.DueSoon => 1,
        FlagKinds.NotRecorded => 2,
        _ => 3,
    };

    private async Task<SafeguardingRecord> RecordAsync(SafeguardingEntry entry, CancellationToken cancellationToken)
    {
        SafeguardingRecord record = Record(entry);
        if (entry.UpdatedBy is { } by)
            record.UpdatedBy = (await accounts.GetAsync([by], cancellationToken)).GetValueOrDefault(by)?.Name;
        return record;
    }

    internal static SafeguardingRecord Record(SafeguardingEntry entry) => new()
    {
        CardNumber = entry.CardNumber ?? "",
        CardStatus = entry.CardStatus ?? "",
        CardExpires = WireDates.ToWire(entry.CardExpires),
        CardChecked = WireDates.ToWire(entry.CardChecked),
        TrainingDone = WireDates.ToWire(entry.TrainingDone),
        TrainingDue = WireDates.ToWire(entry.TrainingDue),
        Source = entry.Source,
        UpdatedAt = entry.UpdatedAt,
    };

    private async Task<Dictionary<Guid, PersonRecord>> RecordsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.People.AsNoTracking().Where(person => ids.Contains(person.UserId)).ToDictionaryAsync(person => person.UserId, cancellationToken);

    private static List<PersonCard> Cards(IEnumerable<FamilyLink> links, Func<FamilyLink, Guid> who,
        IReadOnlyDictionary<Guid, PersonAccount> people, Dictionary<Guid, PersonRecord> records) =>
        links.Where(link => people.TryGetValue(who(link), out PersonAccount? account) && !account.Deleted)
            .Select(link =>
            {
                PersonAccount person = people[who(link)];
                return new PersonCard
                {
                    Id = person.Id.ToString(),
                    Name = person.Name,
                    BirthDate = WireDates.ToWire(records.GetValueOrDefault(person.Id)?.BirthDate),
                    CanSignIn = person.CanSignIn,
                    AddedByFamily = person.AddedByFamily,
                    LinkedAt = link.LinkedAt,
                    How = link.How,
                    LinkedBy = link.LinkedBy is { } by ? people.GetValueOrDefault(by)?.Name : null,
                };
            })
            .OrderBy(card => card.BirthDate ?? DateTime.MaxValue)
            .ThenBy(card => card.Name)
            .ToList();
}

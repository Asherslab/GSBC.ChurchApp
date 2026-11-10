using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class EventsPersonData(EventsDbContext db, EventsPublisher publisher, TimeProvider time) : IPersonData
{
    public async Task ForgetAsync(Guid userId, CancellationToken cancellationToken)
    {
        await db.CalendarLinks.Where(link => link.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        db.FeedChanges.Add(new GSBC.ChurchApp.Platform.Data.FeedChangeRecord
        {
            Feed = EventsFeeds.Mine, RecordId = userId.ToString(), ScopeId = userId.ToString(), Version = time.GetUtcNow().UtcDateTime.Ticks, Deleted = true,
        });

        string personId = userId.ToString();
        DateTime now = time.GetUtcNow().UtcDateTime;
        bool changed = false;
        foreach (EventRecord record in await db.Events.ToListAsync(cancellationToken))
            if (WithoutContact(record.Fields, personId))
            {
                record.UpdatedAt = now;
                publisher.MarkAdmin(record.CalendarId, record.Id);
                changed = true;
            }
        Dictionary<Guid, RegularEventRecord> regulars = await db.RegularEvents.ToDictionaryAsync(record => record.Id, cancellationToken);
        foreach (RegularEventRecord record in regulars.Values)
            if (WithoutContact(record.Fields, personId))
            {
                record.UpdatedAt = now;
                publisher.MarkAdmin(record.CalendarId, record.Id);
                changed = true;
            }
        foreach (SpecialRecord special in await db.Specials.ToListAsync(cancellationToken))
            if (WithoutContact(special.Fields, personId) && regulars.TryGetValue(special.RegularEventId, out RegularEventRecord? regular))
            {
                special.UpdatedAt = now;
                publisher.MarkAdmin(regular.CalendarId, regular.Id);
                changed = true;
            }

        await db.SaveChangesAsync(cancellationToken);
        if (changed)
            await publisher.PublishAsync(cancellationToken);
    }

    private static bool WithoutContact(EventFields fields, string personId) => fields.Contacts.RemoveAll(contact => contact.PersonId == personId) > 0;
}

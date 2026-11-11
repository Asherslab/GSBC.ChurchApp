using GSBC.ChurchApp.Features.Feedback.Api.Data;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Feedback.Api;

internal sealed class FeedbackPersonData(FeedbackDbContext db, FeedbackMedia media, TimeProvider time) : IPersonData
{
    public async Task ForgetAsync(Guid userId, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<TicketRecord> theirs = await db.Tickets.Where(ticket => ticket.UserId == userId).ToListAsync(cancellationToken);
        List<(Guid Ticket, string MediaId)> phoneData = [];
        foreach (TicketRecord ticket in theirs)
        {
            if (ticket.PhoneData is { } data)
                phoneData.Add((ticket.Id, FeedbackPayloads.Decode<MediaRef>(data).Id));
            ticket.UserId = null;
            ticket.SenderName = "";
            ticket.PhoneData = null;
            ticket.ChangedAt = now;
            FeedbackMarks.Changed(db, ticket.Id, now);
        }

        await db.Notes.Where(note => note.AuthorId == userId).ExecuteUpdateAsync(note => note.SetProperty(row => row.AuthorId, (Guid?)null), cancellationToken);
        await db.Steps.Where(step => step.WhoId == userId).ExecuteUpdateAsync(step => step.SetProperty(row => row.WhoId, (Guid?)null), cancellationToken);
        await db.Blocks.Where(block => block.BlockedBy == userId).ExecuteUpdateAsync(block => block.SetProperty(row => row.BlockedBy, (Guid?)null), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        foreach ((Guid ticket, string mediaId) in phoneData)
            await media.ForgetPhoneDataAsync(ticket, mediaId, cancellationToken);
    }
}

using GSBC.ChurchApp.Features.Feedback.Api.Data;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Feedback.Api;

internal sealed class FeedbackKeeping(FeedbackDbContext db, FeedbackDesk desk, FeedbackMedia media, HandledRequests handled, TimeProvider time)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        DateTime closedBefore = now - FeedbackRules.KeptAfterClosing;
        List<TicketRecord> old = await db.Tickets.Include(ticket => ticket.Attachments)
            .Where(ticket => (ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.NotDoing) && ticket.ClosedAt <= closedBefore)
            .ToListAsync(cancellationToken);
        foreach (TicketRecord ticket in old.Where(ticket => FeedbackRules.IsDueForDeletion(ticket, now)))
            await desk.RemoveAsync(ticket, cancellationToken);

        await media.ForgetUnsentAsync(now - FeedbackRules.UnsentUploadsKeptFor, cancellationToken);
        await db.UploadAsks.Where(ask => ask.At < now - FeedbackRules.Day).ExecuteDeleteAsync(cancellationToken);
        await handled.ForgetOldAsync(cancellationToken);
    }
}

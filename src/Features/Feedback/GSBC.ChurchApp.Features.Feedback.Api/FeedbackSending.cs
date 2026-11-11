using Grpc.Core;
using GSBC.ChurchApp.Features.Feedback.Api.Data;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace GSBC.ChurchApp.Features.Feedback.Api;

internal sealed class FeedbackSending(
    FeedbackDbContext db, FeedbackSenders senders, FeedbackMedia media, FeedbackTelling telling, IPeopleIndex people, IHostEnvironment environment, TimeProvider time)
{
    public async Task SendAsync(Sender sender, FeedbackSend send, CancellationToken cancellationToken)
    {
        Guid id = Guid.TryParse(send.Id, out Guid parsed) && parsed != Guid.Empty ? parsed : throw Invalid("Feedback needs its id.");
        string text = send.Text.Trim();
        if (text.Length == 0 && send.AttachmentIds.Count == 0)
            throw Invalid("Write something, or add a screenshot or video.");
        if (text.Length > FeedbackRules.LongestText)
            throw Invalid($"Keep it to {FeedbackRules.LongestText} characters.");
        if (send.AttachmentIds.Count > FeedbackRules.MostAttachments)
            throw Invalid($"Add up to {FeedbackRules.MostAttachments} screenshots or videos.");

        if (await db.Tickets.AsNoTracking().FirstOrDefaultAsync(ticket => ticket.Id == id, cancellationToken) is { } existing)
        {
            if (existing.InstallId == sender.Install && existing.UserId == sender.User)
                return;
            throw new RpcException(new Status(StatusCode.AlreadyExists, "That feedback was already sent."));
        }

        switch (FeedbackRules.MaySend(sender, await senders.SentTodayAsync(sender, cancellationToken)))
        {
            case SendVerdict.NotHere or SendVerdict.Blocked:
                throw new RpcException(new Status(StatusCode.PermissionDenied, FeedbackLimits.NotAvailable));
            case SendVerdict.TooMany:
                throw new RpcException(new Status(StatusCode.ResourceExhausted,
                    $"This phone has sent {FeedbackRules.DailyLimit(sender)} today, the most for a day. It sends this one tomorrow."));
        }

        IReadOnlyList<MediaRef> attachments = await media.VerifyAsync(send.AttachmentIds.Distinct().ToList(), cancellationToken);
        MediaRef? phoneData = send.PhoneDataId.Length > 0 ? (await media.VerifyAsync([send.PhoneDataId], cancellationToken)).SingleOrDefault() : null;
        DateTime now = time.GetUtcNow().UtcDateTime;
        PhoneDetails phone = send.Phone ?? new PhoneDetails();
        TicketRecord ticket = new()
        {
            Id = id,
            Text = text,
            SentAt = now,
            ChangedAt = now,
            InstallId = sender.Install,
            UserId = sender.User,
            SenderName = sender.User is { } person ? (await people.NamesAsync([person], cancellationToken)).GetValueOrDefault(person, "") : "",
            AppVersion = FeedbackRules.Clip(phone.AppVersion, FeedbackRules.LongestDetail),
            AppBuild = FeedbackRules.Clip(phone.AppBuild, FeedbackRules.LongestDetail),
            Platform = FeedbackRules.Clip(phone.Platform, FeedbackRules.LongestDetail),
            OsVersion = FeedbackRules.Clip(phone.OsVersion, FeedbackRules.LongestDetail),
            Model = FeedbackRules.Clip(phone.Model, FeedbackRules.LongestDetail),
            DevelopmentBuild = phone.DevelopmentBuild,
            Environment = environment.EnvironmentName,
            Page = FeedbackRules.Clip(send.Page, FeedbackRules.LongestPage),
            State = FeedbackRules.Clip(send.State, FeedbackRules.LongestState),
            PhoneData = phoneData is null ? null : FeedbackPayloads.Encode(phoneData),
            HadPhoneData = phoneData is not null,
        };
        ticket.Attachments.AddRange(attachments.Select((attachment, position) =>
            new AttachmentRecord { Position = position, MediaId = attachment.Id, Media = FeedbackPayloads.Encode(attachment) }));
        ticket.Steps.Add(new StepRecord { At = now, What = "Sent" });
        db.Tickets.Add(ticket);
        FeedbackMarks.Changed(db, ticket.Id, now);
        await db.SaveChangesAsync(cancellationToken);
        await media.KeepAsync(ticket, attachments, phoneData, cancellationToken);
        await telling.NewAsync(ticket, cancellationToken);
    }

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));
}

using Grpc.Core;
using GSBC.ChurchApp.Features.Prayer.Api.Data;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Prayer.Api;

internal sealed class PrayerRequests(
    PrayerDbContext db,
    PrayerPublisher publisher,
    PrayerTelling telling,
    IPeopleIndex people,
    IFamilies families,
    TimeProvider time)
{
    public async Task SendAsync(Guid install, Guid? user, PrayerRequestSend send, CancellationToken cancellationToken)
    {
        Guid id = IdOf(send.Id);
        string text = send.Text.Trim();
        if (text.Length == 0 || text.Length > PrayerRules.LongestText)
            throw Invalid($"Write a request of up to {PrayerRules.LongestText} characters.");

        if (await db.Requests.AsNoTracking().FirstOrDefaultAsync(request => request.Id == id, cancellationToken) is { } existing)
        {
            if (PrayerRules.IsTheirs(existing, install, user))
                return;
            throw new RpcException(new Status(StatusCode.AlreadyExists, "That request was already sent."));
        }

        DateTime now = time.GetUtcNow().UtcDateTime;
        int sentToday = await db.Requests.CountAsync(request => request.InstallId == install && request.SentAt > now - PrayerRules.Day, cancellationToken);
        if (!PrayerRules.MaySendMore(sentToday))
            throw new RpcException(new Status(StatusCode.ResourceExhausted,
                $"This phone has sent {PrayerRules.RequestsPerDay} prayer requests today. It sends this one tomorrow."));

        RequestRecord record = new()
        {
            Id = id,
            Text = text,
            SentAt = now,
            ChangedAt = now,
            InstallId = install,
            UserId = user,
            Name = await NameAsync(user, send.Name, cancellationToken),
            WantsContact = send.WantsContact,
            Contact = send.WantsContact ? PrayerRules.Clip(send.Contact, PrayerRules.LongestContact) : "",
            Minor = user is { } person && (await families.FactsAsync([person], cancellationToken)).TryGetValue(person, out FamilyFacts? facts) && facts.Minor,
            ShowName = send.ShowName,
            Wall = send.ForWall ? WallState.Waiting : WallState.NotAsked,
            WallText = text,
            WallShowsName = send.ShowName,
        };
        record.Steps.Add(new StepRecord { At = now, What = send.ForWall ? "Sent, asking for the prayer wall" : "Sent" });
        db.Requests.Add(record);
        await publisher.ChangedAsync(record, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await telling.NewRequestAsync(record, cancellationToken);
    }

    public async Task PrayAsync(Guid install, Guid? user, PrayMark mark, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(mark.Id, out Guid id))
            return;
        DateTime now = time.GetUtcNow().UtcDateTime;
        if (await db.Requests.FirstOrDefaultAsync(request => request.Id == id, cancellationToken) is not { } record || !PrayerRules.IsOnTheWall(record, now))
            return;

        PrayedRecord? prayed = await db.Prayed.FirstOrDefaultAsync(row => row.RequestId == id && row.InstallId == install, cancellationToken);
        if (mark.Prayed == prayed is not null)
            return;

        if (mark.Prayed)
        {
            int prayedToday = await db.Prayed.CountAsync(row => row.InstallId == install && row.At > now - PrayerRules.Day, cancellationToken);
            if (!PrayerRules.MayPrayMore(prayedToday))
                return;
            db.Prayed.Add(new PrayedRecord { RequestId = id, InstallId = install, UserId = user, At = now });
            record.Prayed++;
        }
        else
        {
            db.Prayed.Remove(prayed!);
            record.Prayed = Math.Max(0, record.Prayed - 1);
        }

        await publisher.PublishAsync(record, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (mark.Prayed)
            await telling.PrayedForAsync(record, user, cancellationToken);
    }

    public async Task AnsweredAsync(Guid install, Guid? user, AnsweredSave save, CancellationToken cancellationToken)
    {
        if (await TheirsAsync(install, user, save.Id, cancellationToken) is not { } record)
            return;
        DateTime now = time.GetUtcNow().UtcDateTime;
        record.Care = CareState.Answered;
        record.AnsweredNote = PrayerRules.Clip(save.Note, PrayerRules.LongestAnsweredNote);
        record.ChangedAt = now;
        record.Steps.Add(new StepRecord { At = now, What = "They said it was answered" });
        await publisher.ChangedAsync(record, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task TakeDownAsync(Guid install, Guid? user, RequestRef request, CancellationToken cancellationToken)
    {
        if (await TheirsAsync(install, user, request.Id, cancellationToken) is not { } record
            || record.Wall is not (WallState.Waiting or WallState.Shown))
            return;
        DateTime now = time.GetUtcNow().UtcDateTime;
        record.Wall = WallState.TakenDown;
        record.ChangedAt = now;
        record.Steps.Add(new StepRecord { At = now, What = "They took it off the prayer wall" });
        await publisher.ChangedAsync(record, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await telling.SettledAsync(record, cancellationToken);
    }

    public async Task DeleteAsync(Guid install, Guid? user, RequestRef request, CancellationToken cancellationToken)
    {
        if (await TheirsAsync(install, user, request.Id, cancellationToken) is not { } record)
            return;
        DateTime now = time.GetUtcNow().UtcDateTime;
        db.Requests.Remove(record);
        publisher.Deleted(record, now);
        await db.SaveChangesAsync(cancellationToken);
        await telling.DeletedAsync(record, cancellationToken);
    }

    private async Task<RequestRecord?> TheirsAsync(Guid install, Guid? user, string id, CancellationToken cancellationToken) =>
        Guid.TryParse(id, out Guid requestId)
        && await db.Requests.FirstOrDefaultAsync(request => request.Id == requestId, cancellationToken) is { } record
        && PrayerRules.IsTheirs(record, install, user)
            ? record
            : null;

    private async Task<string> NameAsync(Guid? user, string typed, CancellationToken cancellationToken) =>
        user is { } person
            ? (await people.NamesAsync([person], cancellationToken)).GetValueOrDefault(person, "")
            : PrayerRules.Clip(typed, PrayerRules.LongestName);

    private static Guid IdOf(string id) =>
        Guid.TryParse(id, out Guid parsed) && parsed != Guid.Empty ? parsed : throw Invalid("A prayer request needs its id.");


    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));
}

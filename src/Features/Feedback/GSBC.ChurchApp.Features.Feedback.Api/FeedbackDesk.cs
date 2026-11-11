using Grpc.Core;
using GSBC.ChurchApp.Features.Feedback.Api.Data;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Feedback.Api;

internal sealed class FeedbackDesk(
    FeedbackDbContext db, FeedbackMedia media, FeedbackTelling telling, HandledRequests handled, IPeopleIndex people, TimeProvider time)
{
    public async Task<DeskList> ListAsync(DeskQuery query, CancellationToken cancellationToken)
    {
        IQueryable<TicketRecord> tickets = db.Tickets.AsNoTracking().Include(ticket => ticket.Attachments);
        tickets = query.Filter switch
        {
            DeskFilter.Open => tickets.Where(ticket => ticket.Status != TicketStatus.Resolved && ticket.Status != TicketStatus.NotDoing),
            DeskFilter.Closed => tickets.Where(ticket => ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.NotDoing),
            _ => tickets,
        };
        string search = query.Search.Trim();
        if (search.Length > 0)
            tickets = int.TryParse(search.TrimStart('#'), out int number)
                ? tickets.Where(ticket => ticket.Number == number)
                : tickets.Where(ticket => EF.Functions.ILike(ticket.Text, $"%{search}%") || EF.Functions.ILike(ticket.SenderName, $"%{search}%"));

        List<TicketRecord> found = await tickets.OrderByDescending(ticket => ticket.SentAt).ToListAsync(cancellationToken);
        List<StatusCount> counts = (await db.Tickets.AsNoTracking().GroupBy(ticket => ticket.Status)
                .Select(group => new { Status = group.Key, Count = group.Count() }).ToListAsync(cancellationToken))
            .Select(group => new StatusCount { Status = group.Status, Count = group.Count }).ToList();
        return new DeskList { Tickets = found.Select(Line).ToList(), Counts = counts };
    }

    public async Task<Ticket> TicketAsync(TicketRef reference, bool mayReadPhoneData, CancellationToken cancellationToken)
    {
        TicketRecord ticket = await FindAsync(reference, tracking: false, cancellationToken);
        HashSet<Guid> named = ticket.Notes.Select(note => note.AuthorId).Concat(ticket.Steps.Select(step => step.WhoId)).OfType<Guid>().ToHashSet();
        IReadOnlyDictionary<Guid, string> names = named.Count == 0 ? new Dictionary<Guid, string>() : await people.NamesAsync(named, cancellationToken);
        BlockRecord? block = ticket.InstallId is { } phone ? await db.Blocks.AsNoTracking().FirstOrDefaultAsync(row => row.InstallId == phone, cancellationToken) : null;
        return new Ticket
        {
            Line = Line(ticket),
            Attachments = ticket.Attachments.OrderBy(attachment => attachment.Position).Select(attachment => FeedbackPayloads.Decode<MediaRef>(attachment.Media)).ToList(),
            Phone = new PhoneDetails
            {
                AppVersion = ticket.AppVersion, AppBuild = ticket.AppBuild, Platform = ticket.Platform, OsVersion = ticket.OsVersion, Model = ticket.Model,
                DevelopmentBuild = ticket.DevelopmentBuild,
            },
            Environment = ticket.Environment,
            Page = ticket.Page,
            State = ticket.State,
            PersonId = ticket.UserId?.ToString() ?? "",
            InstallId = ticket.InstallId?.ToString() ?? "",
            FromThisPhone = ticket.InstallId is { } install ? await db.Tickets.CountAsync(other => other.InstallId == install, cancellationToken) : 0,
            Blocked = block is not null,
            BlockedBecause = block?.Reason ?? "",
            Notes = ticket.Notes.OrderBy(note => note.At)
                .Select(note => new TicketNote { Id = note.Id.ToString(), Text = note.Text, Author = NameOf(names, note.AuthorId), At = note.At }).ToList(),
            History = ticket.Steps.OrderBy(step => step.At).ThenBy(step => step.Id)
                .Select(step => new TicketStep { What = step.What, Who = step.WhoId is null ? FeedbackRules.From(ticket) : NameOf(names, step.WhoId), At = step.At }).ToList(),
            PhoneData = mayReadPhoneData && ticket.PhoneData is { } data ? FeedbackPayloads.Decode<MediaRef>(data) : null,
            HadPhoneData = ticket.HadPhoneData,
            ClosedAt = ticket.ClosedAt,
        };
    }

    public async Task SetStatusAsync(Guid staff, StatusChange change, string? queuedId, CancellationToken cancellationToken)
    {
        if (await handled.AlreadyAsync(queuedId, cancellationToken))
            return;
        TicketRecord ticket = await FindAsync(new TicketRef { Id = change.Id }, tracking: true, cancellationToken);
        string note = FeedbackRules.Clip(change.Note, FeedbackRules.LongestNote);
        if (ticket.Status == change.Status && note.Length == 0)
            return;
        DateTime now = time.GetUtcNow().UtcDateTime;
        bool closing = FeedbackRules.IsClosed(change.Status) && !FeedbackRules.IsClosed(ticket.Status);
        bool leavingNew = ticket.Status == TicketStatus.New && change.Status != TicketStatus.New;
        if (ticket.Status != change.Status)
        {
            ticket.Status = change.Status;
            ticket.ClosedAt = FeedbackRules.IsClosed(change.Status) ? ticket.ClosedAt ?? now : null;
            Step(ticket, staff, now, $"Moved to {FeedbackRules.StatusName(change.Status)}");
        }
        if (note.Length > 0)
            db.Notes.Add(new NoteRecord { Id = Guid.NewGuid(), TicketId = ticket.Id, AuthorId = staff, Text = note, At = now });
        List<string> attachments = closing ? ticket.Attachments.Select(attachment => attachment.MediaId).ToList() : [];
        string? phoneData = closing && ticket.PhoneData is { } data ? FeedbackPayloads.Decode<MediaRef>(data).Id : null;
        if (closing)
            ClearMedia(ticket, staff, now);
        ticket.ChangedAt = now;
        FeedbackMarks.Changed(db, ticket.Id, now);
        await db.SaveChangesAsync(cancellationToken);
        if (closing)
        {
            await media.ForgetAttachmentsAsync(ticket.Id, attachments, cancellationToken);
            await media.ForgetPhoneDataAsync(ticket.Id, phoneData, cancellationToken);
        }
        if (leavingNew)
            await telling.SettledAsync(ticket, cancellationToken);
    }

    public async Task AddNoteAsync(Guid staff, NoteSave save, CancellationToken cancellationToken)
    {
        string text = save.Text.Trim();
        if (text.Length == 0 || text.Length > FeedbackRules.LongestNote)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Write a note of up to {FeedbackRules.LongestNote} characters."));
        TicketRecord ticket = await FindAsync(new TicketRef { Id = save.TicketId }, tracking: true, cancellationToken);
        DateTime now = time.GetUtcNow().UtcDateTime;
        db.Notes.Add(new NoteRecord { Id = Guid.NewGuid(), TicketId = ticket.Id, AuthorId = staff, Text = text, At = now });
        ticket.ChangedAt = now;
        FeedbackMarks.Changed(db, ticket.Id, now);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteNoteAsync(Guid staff, NoteRef reference, CancellationToken cancellationToken)
    {
        TicketRecord ticket = await FindAsync(new TicketRef { Id = reference.TicketId }, tracking: true, cancellationToken);
        if (!Guid.TryParse(reference.NoteId, out Guid noteId) || ticket.Notes.FirstOrDefault(candidate => candidate.Id == noteId) is not { } note)
            return;
        DateTime now = time.GetUtcNow().UtcDateTime;
        db.Notes.Remove(note);
        Step(ticket, staff, now, "Deleted a note");
        ticket.ChangedAt = now;
        FeedbackMarks.Changed(db, ticket.Id, now);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TicketRef reference, CancellationToken cancellationToken)
    {
        if (await FindOrNullAsync(reference, tracking: true, cancellationToken) is not { } ticket)
            return;
        await RemoveAsync(ticket, cancellationToken);
    }

    public async Task RemoveAsync(TicketRecord ticket, CancellationToken cancellationToken)
    {
        List<string> attachments = ticket.Attachments.Select(attachment => attachment.MediaId).ToList();
        string? phoneData = ticket.PhoneData is { } data ? FeedbackPayloads.Decode<MediaRef>(data).Id : null;
        db.Tickets.Remove(ticket);
        FeedbackMarks.Changed(db, ticket.Id, time.GetUtcNow().UtcDateTime, deleted: true);
        await db.SaveChangesAsync(cancellationToken);
        await media.ForgetAttachmentsAsync(ticket.Id, attachments, cancellationToken);
        await media.ForgetPhoneDataAsync(ticket.Id, phoneData, cancellationToken);
        await telling.SettledAsync(ticket, cancellationToken);
    }

    public async Task BlockAsync(Guid staff, BlockSave save, CancellationToken cancellationToken)
    {
        TicketRecord ticket = await FindAsync(new TicketRef { Id = save.TicketId }, tracking: true, cancellationToken);
        Guid install = ticket.InstallId
            ?? throw new RpcException(new Status(StatusCode.FailedPrecondition, "This feedback didn't come from a verified phone, so there's no phone to block."));
        string reason = FeedbackRules.Clip(save.Reason, FeedbackRules.LongestReason);
        if (reason.Length == 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Say why the phone is blocked."));
        DateTime now = time.GetUtcNow().UtcDateTime;
        if (await db.Blocks.FirstOrDefaultAsync(block => block.InstallId == install, cancellationToken) is { } existing)
            existing.Reason = reason;
        else
            db.Blocks.Add(new BlockRecord { InstallId = install, Reason = reason, BlockedBy = staff, BlockedAt = now });
        Step(ticket, staff, now, "Blocked this phone");
        FeedbackMarks.Changed(db, ticket.Id, now);
        FeedbackMarks.PhoneChanged(db, install, now);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnblockAsync(PhoneRef phone, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(phone.InstallId, out Guid install)
            || await db.Blocks.FirstOrDefaultAsync(row => row.InstallId == install, cancellationToken) is not { } block)
            return;
        db.Blocks.Remove(block);
        FeedbackMarks.PhoneChanged(db, install, time.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<BlockedPhones> BlockedAsync(CancellationToken cancellationToken)
    {
        List<BlockRecord> blocks = await db.Blocks.AsNoTracking().OrderByDescending(block => block.BlockedAt).ToListAsync(cancellationToken);
        HashSet<Guid> staff = blocks.Select(block => block.BlockedBy).OfType<Guid>().ToHashSet();
        IReadOnlyDictionary<Guid, string> names = staff.Count == 0 ? new Dictionary<Guid, string>() : await people.NamesAsync(staff, cancellationToken);
        BlockedPhones phones = new();
        foreach (BlockRecord block in blocks)
        {
            List<TicketRecord> sent = await db.Tickets.AsNoTracking().Where(ticket => ticket.InstallId == block.InstallId)
                .OrderByDescending(ticket => ticket.SentAt).ToListAsync(cancellationToken);
            phones.Phones.Add(new BlockedPhone
            {
                InstallId = block.InstallId.ToString(), Reason = block.Reason, BlockedAt = block.BlockedAt, BlockedBy = NameOf(names, block.BlockedBy),
                Sent = sent.Count, Model = sent.FirstOrDefault()?.Model ?? "", LastNumber = sent.FirstOrDefault()?.Number ?? 0,
            });
        }
        return phones;
    }

    private void ClearMedia(TicketRecord ticket, Guid staff, DateTime now)
    {
        if (ticket.Attachments.Count == 0 && ticket.PhoneData is null)
            return;
        db.Attachments.RemoveRange(ticket.Attachments);
        ticket.PhoneData = null;
        ticket.AttachmentsDeleted = true;
        Step(ticket, staff, now, "Its pictures, videos and phone data were deleted");
    }

    private void Step(TicketRecord ticket, Guid? staff, DateTime now, string what) =>
        db.Steps.Add(new StepRecord { TicketId = ticket.Id, WhoId = staff, At = now, What = what });

    private async Task<TicketRecord> FindAsync(TicketRef reference, bool tracking, CancellationToken cancellationToken) =>
        await FindOrNullAsync(reference, tracking, cancellationToken) ?? throw new RpcException(new Status(StatusCode.NotFound, "That feedback isn't here any more."));

    private async Task<TicketRecord?> FindOrNullAsync(TicketRef reference, bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<TicketRecord> tickets = (tracking ? db.Tickets : db.Tickets.AsNoTracking())
            .Include(ticket => ticket.Attachments).Include(ticket => ticket.Notes).Include(ticket => ticket.Steps);
        if (Guid.TryParse(reference.Id, out Guid id))
            return await tickets.FirstOrDefaultAsync(ticket => ticket.Id == id, cancellationToken);
        return reference.Number > 0 ? await tickets.FirstOrDefaultAsync(ticket => ticket.Number == reference.Number, cancellationToken) : null;
    }

    private static string NameOf(IReadOnlyDictionary<Guid, string> names, Guid? who) => who is { } id ? names.GetValueOrDefault(id, "Someone") : "Someone";

    private static TicketLine Line(TicketRecord ticket) => new()
    {
        Id = ticket.Id.ToString(),
        Number = ticket.Number,
        Text = ticket.Text,
        From = FeedbackRules.From(ticket),
        SentAt = ticket.SentAt,
        Status = ticket.Status,
        Attachments = ticket.Attachments.Count,
        Cover = ticket.Attachments.OrderBy(attachment => attachment.Position).Select(attachment => FeedbackPayloads.Decode<MediaRef>(attachment.Media))
            .FirstOrDefault(attachment => attachment.IsImage),
        AppBuild = ticket.AppVersion.Length > 0 ? $"{ticket.AppVersion} ({ticket.AppBuild})" : "",
        Model = ticket.Model,
        ChangedAt = ticket.ChangedAt,
        AttachmentsDeleted = ticket.AttachmentsDeleted,
    };
}

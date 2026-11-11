using System.Security.Claims;
using GSBC.ChurchApp.Features.Feedback.Api.Data;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Feedback.Api;

internal sealed class FeedbackSenders(FeedbackDbContext db, IAccessEvaluator access, AdminPanelAccess adminPanel, TimeProvider time)
{
    public async Task<Sender> WhoAsync(ClaimsPrincipal user, Guid? install, FeedbackSource source, CancellationToken cancellationToken)
    {
        Guid? person = Caller.UserId(user);
        bool holdsTheRole = person is not null && await access.IsAllowedAsync(user, FeedbackPermissions.SendNode, null, cancellationToken);
        bool blocked = install is { } phone && await db.Blocks.AsNoTracking().AnyAsync(block => block.InstallId == phone, cancellationToken);
        bool usesTheAdminPanel = source == FeedbackSource.AdminPanel && await adminPanel.MayUseAsync(user, cancellationToken);
        return new Sender(install, person, holdsTheRole, blocked, usesTheAdminPanel);
    }

    public async Task<int> SentTodayAsync(Sender sender, CancellationToken cancellationToken)
    {
        DateTime since = time.GetUtcNow().UtcDateTime - FeedbackRules.Day;
        IQueryable<TicketRecord> recent = db.Tickets.AsNoTracking().Where(ticket => ticket.SentAt > since);
        return sender is { CountsByPerson: true, User: { } person }
            ? await recent.CountAsync(ticket => ticket.UserId == person, cancellationToken)
            : await recent.CountAsync(ticket => ticket.InstallId == sender.Install, cancellationToken);
    }

    public async Task<bool> MayUploadFromInstallAsync(Guid install, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        Sender sender = new(install, null, false, await db.Blocks.AsNoTracking().AnyAsync(block => block.InstallId == install, cancellationToken));
        if (FeedbackRules.MaySend(sender, await SentTodayAsync(sender, cancellationToken)) != SendVerdict.Allowed
            || await db.UploadAsks.CountAsync(ask => ask.InstallId == install && ask.At > now - FeedbackRules.Day, cancellationToken) >= FeedbackRules.UploadAsksADay)
            return false;
        db.UploadAsks.Add(new UploadAskRecord { InstallId = install, At = now });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal abstract class FeedbackAreaAccess(FeedbackSenders senders, IAccessEvaluator access, AdminPanelAccess adminPanel) : IMediaAreaAccess
{
    public abstract string Area { get; }

    protected abstract string ReadNode { get; }

    public async Task<bool> MayUploadAsync(ClaimsPrincipal user, string scopeId, CancellationToken cancellationToken) =>
        Caller.UserId(user) is not null
        && (await access.IsAllowedAsync(user, FeedbackPermissions.SendNode, null, cancellationToken) || await adminPanel.MayUseAsync(user, cancellationToken));

    public async Task<bool> MayReadAsync(ClaimsPrincipal user, string scopeId, CancellationToken cancellationToken) =>
        await access.IsAllowedAsync(user, ReadNode, null, cancellationToken);

    public Task<bool> MayUploadFromInstallAsync(Guid install, string scopeId, CancellationToken cancellationToken) =>
        senders.MayUploadFromInstallAsync(install, cancellationToken);
}

internal sealed class FeedbackAttachmentAccess(FeedbackSenders senders, IAccessEvaluator access, AdminPanelAccess adminPanel)
    : FeedbackAreaAccess(senders, access, adminPanel)
{
    public override string Area => FeedbackAreas.Attachments;

    protected override string ReadNode => FeedbackPermissions.TriageNode;
}

internal sealed class FeedbackPhoneDataAccess(FeedbackSenders senders, IAccessEvaluator access, AdminPanelAccess adminPanel)
    : FeedbackAreaAccess(senders, access, adminPanel)
{
    public override string Area => FeedbackAreas.PhoneData;

    protected override string ReadNode => FeedbackPermissions.PhoneDataNode;
}

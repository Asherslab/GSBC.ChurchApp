using System.Net.Mail;
using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Email;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.People.Api;

internal sealed class PeopleService(
    PeopleDbContext db,
    Families families,
    FamilyViews views,
    IPeopleAccounts accounts,
    IAccountEmails emails,
    IAccessEvaluator access,
    INotifications notifications,
    TimeProvider time) : IPeopleService
{
    private const int MostWaitingAsks = 5;
    private const int LongestCardNumber = 30;
    private const int OldestAge = 120;

    private DateTime Now => time.GetUtcNow().UtcDateTime;
    private DateOnly Today => ChurchTime.Today(time);

    public async ValueTask<MyPeople> GetMineAsync(MineRequest request, CallContext context = default)
    {
        return new MyPeople { Me = await views.FamilyAsync(Me(context), Shown.Own, context.CancellationToken) };
    }

    public async ValueTask<Done> SetMyBirthDateAsync(BirthDateRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = Me(context);
        PersonRecord? person = await db.People.SingleOrDefaultAsync(found => found.UserId == me, cancellationToken);
        if (!FamilyRules.MaySetOwnBirthDate(person))
            throw Refused("Your birth date is already set. Ask the church office if it's wrong.");

        person ??= db.People.Add(new PersonRecord { UserId = me }).Entity;
        if (request.BirthDate is { } born)
            person.BirthDate = ValidBirthDate(WireDates.FromWire(born));
        else if (request.Adult)
            person.SaysAdult = true;
        else
            throw Invalid("Give your birth date, or say you're 18 or older.");

        person.UpdatedAt = Now;
        await families.TouchAsync([me], cancellationToken: cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await families.RefreshParentRolesAsync(me, cancellationToken);
        return new Done();
    }

    public async ValueTask<PersonCard> AddChildAsync(AddChildRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = Me(context);
        Guid parentId = request.ParentId.Length == 0 ? me : ParseId(request.ParentId);
        bool forSomeoneElse = parentId != me;
        if (forSomeoneElse)
            await RequireAsync(context, PeoplePermissions.FamiliesManageNode, null, cancellationToken);

        DateOnly born = ValidBirthDate(WireDates.FromWire(request.BirthDate));
        if (!Ages.IsMinor(born, Today))
            throw Invalid("Add children under 18 here. An adult signs up with their own email.");

        Guid childId = await accounts.AddWithoutSignInAsync(request.Name, me, cancellationToken);
        db.People.Add(new PersonRecord { UserId = childId, BirthDate = born, UpdatedAt = Now });
        await db.SaveChangesAsync(cancellationToken);
        await families.LinkAsync(parentId, childId, forSomeoneElse ? LinkHow.Office : LinkHow.Added, me, cancellationToken);
        return (await views.FamilyAsync(parentId, Shown.Hidden, cancellationToken)).Children.Single(child => child.Id == childId.ToString());
    }

    public async ValueTask<Done> AskToLinkAsync(AskRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = Me(context);
        Guid childId = request.ChildId.Length == 0 ? me : ParseId(request.ChildId);
        if (childId != me)
            await RequireAsync(context, PeoplePermissions.FamilyEditNode, childId.ToString(), cancellationToken);

        string email = ValidEmail(request.ParentEmail);
        if ((await NormalizedEmailsAsync(me, cancellationToken)).Contains(email))
            throw Invalid(childId == me ? "That's your own email. Use your parent's." : "That's your own email. Use the other parent's.");

        Guid? parentId = await accounts.FindByEmailAsync(email, cancellationToken);
        if (parentId is { } existing && await db.Links.AnyAsync(link => link.ChildId == childId && link.ParentId == existing, cancellationToken))
            return new Done();

        List<LinkAskRecord> asks = await db.Asks.Where(ask => ask.ChildId == childId).ToListAsync(cancellationToken);
        if (asks.All(ask => ask.ParentEmail != email))
        {
            if (asks.Count >= MostWaitingAsks)
                throw Refused($"There are already {MostWaitingAsks} asks waiting. Take one back first.");
            db.Asks.Add(new LinkAskRecord { Id = Guid.CreateVersion7(), ChildId = childId, ParentEmail = email, AskedBy = me, AskedAt = Now });
        }
        await families.TouchAsync([childId, me], cancellationToken: cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        if (parentId is { } parent)
            await TellParentAsync(parent, childId, me, cancellationToken);
        return new Done();
    }

    public async ValueTask<Done> AnswerAsync(AnswerRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = Me(context);
        LinkAskRecord ask = await FindAskAsync(request.AskId, cancellationToken);
        if (!(await NormalizedEmailsAsync(me, cancellationToken)).Contains(ask.ParentEmail))
            throw Refused("This ask is for someone else.");

        db.Asks.Remove(ask);
        await families.TouchAsync([ask.ChildId, ask.AskedBy, me], cancellationToken: cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (request.Accept)
            await families.LinkAsync(me, ask.ChildId, LinkHow.Approved, me, cancellationToken);
        await notifications.SettleAsync(AskSubject(ask.ChildId, me), cancellationToken);
        return new Done();
    }

    public async ValueTask<Done> WithdrawAsync(AskIdRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = Me(context);
        LinkAskRecord ask = await FindAskAsync(request.AskId, cancellationToken);
        if (me != ask.AskedBy && me != ask.ChildId && !await AllowedAsync(context, PeoplePermissions.FamiliesManageNode, null, cancellationToken))
            throw Refused("Only whoever asked, or the church office, can take this back.");

        db.Asks.Remove(ask);
        await families.TouchAsync([ask.ChildId, ask.AskedBy], cancellationToken: cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (await accounts.FindByEmailAsync(ask.ParentEmail, cancellationToken) is { } parent)
            await notifications.SettleAsync(AskSubject(ask.ChildId, parent), cancellationToken);
        return new Done();
    }

    public async ValueTask<Done> UnlinkAsync(LinkRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = Me(context);
        (Guid parentId, Guid childId) = (ParseId(request.ParentId), ParseId(request.ChildId));
        FamilyLink link = await db.Links.SingleOrDefaultAsync(found => found.ParentId == parentId && found.ChildId == childId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "They aren't linked."));

        DateOnly? born = (await db.People.AsNoTracking().SingleOrDefaultAsync(person => person.UserId == childId, cancellationToken))?.BirthDate;
        bool office = await AllowedAsync(context, PeoplePermissions.FamiliesManageNode, null, cancellationToken);
        if (FamilyRules.WhyNotUnlink(me, link, Ages.IsMinor(born, Today), office) is { } problem)
            throw Refused(problem);

        List<FamilyLink> childLinks = await db.Links.AsNoTracking().Where(found => found.ChildId == childId).ToListAsync(cancellationToken);
        IReadOnlyDictionary<Guid, PersonAccount> people = await accounts.GetAsync([childId], cancellationToken);
        if (FamilyRules.LeftAlone(parentId, childLinks, people).Contains(childId))
            throw Refused($"{people[childId].Name} can't sign in themselves, so they need a parent in the app. Remove them instead, or link another parent first.");

        await families.UnlinkAsync(link, cancellationToken);
        return new Done();
    }

    public async ValueTask<PersonFamily> GetPersonAsync(PersonRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        RefuseTools(context);
        Guid personId = ParseId(request.PersonId);
        bool safeguarding = await AllowedAsync(context, PeoplePermissions.SafeguardingViewNode, personId.ToString(), cancellationToken);
        return await views.FamilyAsync(personId, safeguarding ? Shown.Office : Shown.Hidden, cancellationToken);
    }

    public async ValueTask<Done> UpdatePersonAsync(UpdatePersonRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = Me(context);
        RefuseTools(context);
        Guid personId = ParseId(request.PersonId);
        if (request.Name is { } name)
            await accounts.RenameAsync(personId, name, me, cancellationToken);

        if (request.Reach is { } reach)
        {
            if (!Reach.Valid(reach))
                throw Invalid("Choose how to reach them from the list.");
            PersonRecord person = await db.People.SingleOrDefaultAsync(found => found.UserId == personId, cancellationToken)
                ?? db.People.Add(new PersonRecord { UserId = personId }).Entity;
            person.Reach = reach;
            person.UpdatedAt = Now;
        }

        if (request.BirthDate is not null || request.ClearBirthDate)
        {
            PersonRecord person = await db.People.SingleOrDefaultAsync(found => found.UserId == personId, cancellationToken)
                ?? db.People.Add(new PersonRecord { UserId = personId }).Entity;
            person.BirthDate = request.ClearBirthDate ? null : ValidBirthDate(WireDates.FromWire(request.BirthDate!.Value));
            person.SaysAdult = false;
            person.UpdatedAt = Now;
        }
        await families.TouchAsync([personId], cancellationToken: cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await families.RefreshParentRolesAsync(personId, cancellationToken);
        return new Done();
    }

    public async ValueTask<Done> RemoveChildAsync(PersonRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = Me(context);
        RefuseTools(context);
        Guid childId = ParseId(request.PersonId);
        PersonAccount child = (await accounts.GetAsync([childId], cancellationToken)).GetValueOrDefault(childId)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "That person isn't in the app any more."));
        if (!child.AddedByFamily || child.CanSignIn)
            throw Refused($"{child.Name} signs in themselves, so only they or the church office can delete the account. You can stop looking after them instead.");

        await accounts.RemoveAsync(childId, me, cancellationToken);
        return new Done();
    }

    public async ValueTask<SignInCodeReply> MakeSignInCodeAsync(PersonRequest request, CallContext context = default)
    {
        RefuseTools(context);
        FamilySignInCode code = await accounts.MakeSignInCodeAsync(ParseId(request.PersonId), Me(context), context.CancellationToken);
        return new SignInCodeReply { Code = code.Code, ExpiresAt = code.ExpiresAt };
    }

    public async ValueTask<Done> LinkAsync(LinkRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        (Guid parentId, Guid childId) = (ParseId(request.ParentId), ParseId(request.ChildId));
        IReadOnlyList<string> parentEmails = await NormalizedEmailsAsync(parentId, cancellationToken);
        await db.Asks.Where(ask => ask.ChildId == childId && parentEmails.Contains(ask.ParentEmail)).ExecuteDeleteAsync(cancellationToken);
        await families.LinkAsync(parentId, childId, LinkHow.Office, Me(context), cancellationToken);
        await notifications.SettleAsync(AskSubject(childId, parentId), cancellationToken);
        return new Done();
    }

    public async ValueTask<Done> SetSafeguardingAsync(SafeguardingRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = Me(context);
        Guid personId = ParseId(request.PersonId);
        SafeguardingRecord record = request.Record;
        SafeguardingEntry? entry = await db.Safeguarding.SingleOrDefaultAsync(found => found.UserId == personId, cancellationToken);
        if (entry?.Source == SafeguardingSources.SafeMinistryCheck)
            throw Refused("Their blue card and training come from Safe Ministry Check. Change them there, or unlink them to enter them by hand.");

        if (record is { HasCard: false, HasTraining: false })
        {
            if (entry is not null)
                db.Safeguarding.Remove(entry);
        }
        else
        {
            entry ??= db.Safeguarding.Add(new SafeguardingEntry { UserId = personId, Source = SafeguardingSources.ByHand }).Entity;
            Fill(entry, record, me);
        }
        await families.TouchAsync([personId], safeguarding: true, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new Done();
    }

    public async ValueTask<FlaggedList> GetFlaggedAsync(FlaggedRequest request, CallContext context = default)
    {
        RefuseTools(context);
        return new FlaggedList { People = [.. await views.FlaggedAsync(Today, context.CancellationToken)] };
    }

    private void Fill(SafeguardingEntry entry, SafeguardingRecord record, Guid me)
    {
        string number = record.CardNumber.Trim();
        if (number.Length > LongestCardNumber)
            throw Invalid($"A blue card number is up to {LongestCardNumber} characters.");
        string status = record.CardStatus.Trim();
        if (status.Length > 0 && !BlueCards.Valid(status))
            throw Invalid("Choose the blue card's status from the list.");
        DateOnly? done = WireDates.FromWire(record.TrainingDone);
        if (done > Today)
            throw Invalid("Training can't be done in the future.");

        entry.CardNumber = number.Length > 0 ? number : null;
        entry.CardStatus = status.Length > 0 ? status : record.HasCard ? BlueCards.Current : null;
        entry.CardExpires = WireDates.FromWire(record.CardExpires);
        entry.CardChecked = WireDates.FromWire(record.CardChecked);
        entry.TrainingDone = done;
        entry.TrainingDue = FamilyRules.TrainingDue(done, WireDates.FromWire(record.TrainingDue));
        entry.Source = SafeguardingSources.ByHand;
        entry.UpdatedAt = Now;
        entry.UpdatedBy = me;
    }

    private async Task TellParentAsync(Guid parentId, Guid childId, Guid askedBy, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, PersonAccount> names = await accounts.GetAsync([childId, askedBy], cancellationToken);
        string child = names.GetValueOrDefault(childId)?.Name ?? "Someone";
        (string title, string body) = askedBy == childId
            ? ($"{child} asks you to be their parent in the app", "Say yes on your Account page if they're your child, and you'll see their details while they're under 18.")
            : ($"{names.GetValueOrDefault(askedBy)?.Name ?? "A parent"} asks you to look after {child} in the app too", "Say yes on your Account page if you're their parent or guardian.");
        await notifications.RaiseAsync(new Notification(PeopleNotifications.Asked, NotificationAudience.Person(parentId), title, body, "/account",
            Subject: AskSubject(childId, parentId), Action: "Answer"), cancellationToken);
    }

    private static string AskSubject(Guid childId, Guid parentId) => $"people-ask-{childId:N}-{parentId:N}";

    private async Task<LinkAskRecord> FindAskAsync(string id, CancellationToken cancellationToken) =>
        await db.Asks.SingleOrDefaultAsync(ask => ask.Id == ParseId(id), cancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, "That ask was already answered or taken back."));

    private async Task<IReadOnlyList<string>> NormalizedEmailsAsync(Guid userId, CancellationToken cancellationToken) =>
        (await emails.EmailsOfAsync([userId], cancellationToken)).Select(Normalize).ToList();

    private DateOnly ValidBirthDate(DateOnly born) =>
        born > Today ? throw Invalid("A birth date can't be in the future.")
        : Ages.YearsOn(born, Today) > OldestAge ? throw Invalid("Check the year of that birth date.")
        : born;

    private static string ValidEmail(string email) =>
        MailAddress.TryCreate(email.Trim(), out MailAddress? address) && address.Address == email.Trim()
            ? Normalize(address.Address)
            : throw Invalid("That doesn't look like an email address.");

    private static string Normalize(string email) => email.Trim().ToUpperInvariant();

    private async Task RequireAsync(CallContext context, string node, string? scope, CancellationToken cancellationToken)
    {
        if (!await AllowedAsync(context, node, scope, cancellationToken))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Not permitted."));
    }

    private async Task<bool> AllowedAsync(CallContext context, string node, string? scope, CancellationToken cancellationToken) =>
        await access.IsAllowedAsync(User(context), node, scope, cancellationToken);

    private static void RefuseTools(CallContext context)
    {
        if (Caller.IsTool(User(context)))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Connected tools can't see or change families."));
    }

    private static ClaimsPrincipal User(CallContext context) => context.ServerCallContext!.GetHttpContext().User;

    private static Guid Me(CallContext context) =>
        Caller.UserId(User(context)) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in again."));

    private static Guid ParseId(string id) =>
        Guid.TryParse(id, out Guid parsed) ? parsed : throw Invalid("That isn't a person in the app.");

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private static RpcException Refused(string message) => new(new Status(StatusCode.FailedPrecondition, message));
}

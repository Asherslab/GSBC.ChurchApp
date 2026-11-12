using System.Globalization;
using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;
using static GSBC.ChurchApp.Features.Ministries.Api.MinistriesService;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryLeadingService(
    MinistriesDbContext db,
    MinistryViews views,
    MinistryChanges changes,
    MinistryRemover remover,
    MinistryMover mover,
    IRoleAssignments roles,
    IFamilies families,
    IPeopleIndex people,
    IMinistryCalendars calendars,
    IKeptRegularEvents keptEvents,
    INotifications notifications,
    MinistryFileMedia fileMedia,
    TimeProvider time) : IMinistryLeadingService
{
    private const int MostFound = 12;

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public async ValueTask<PeopleList> PeopleAsync(MinistryRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = SignedIn(context);
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        MinistryRecord ministry = Existing(tree, request.MinistryId);

        MinistryHoldings holdings = await views.HoldingsAsync([ministry, .. tree.Above(ministry.Id)], cancellationToken);
        List<Guid> everyone = [.. MinistryPeopleLists.Everyone(ministry, tree, holdings)];
        IReadOnlyDictionary<Guid, FamilyFacts> facts = await views.FactsAsync(everyone, cancellationToken);
        List<Guid> parents = facts.Values.SelectMany(fact => fact.Parents).Distinct().ToList();
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync([.. everyone, .. parents], cancellationToken);

        List<string> mayGive = [];
        foreach (string role in MinistryHoldings.AllRoles)
            if (await roles.MayGiveAsync(me, Roles.KeyOf(role), ministry.Id.ToString(), cancellationToken))
                mayGive.Add(role);

        List<AskRecord> asks = await db.Asks.AsNoTracking().Where(ask => ask.MinistryId == ministry.Id).OrderBy(ask => ask.AskedAt).ToListAsync(cancellationToken);
        return MinistryPeopleLists.Build(ministry, tree, holdings, facts, names, await AskLinesAsync(asks, tree, views, people, cancellationToken), mayGive);
    }

    public async ValueTask<Done> AnswerAsync(AskRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        MinistryRecord ministry = Existing(tree, request.MinistryId);
        Guid id = Guid.TryParse(request.AskId, out Guid parsed) ? parsed : Guid.Empty;
        AskRecord ask = await db.Asks.SingleOrDefaultAsync(found => found.Id == id && found.MinistryId == ministry.Id, cancellationToken)
                        ?? throw new RpcException(new Status(StatusCode.NotFound, "That ask was already answered or taken back."));
        if (request.Yes && ask.WaitingForParent)
            throw Refused("They're under 18, so a parent says yes first. You'll hear when they do.");
        db.Asks.Remove(ask);
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        if (!request.Yes)
            return new Done();

        await roles.GiveAsFeatureAsync(ask.PersonId, Roles.KeyOf(ask.As == Places.Team ? Roles.Team : Roles.Attendee), ministry.Id.ToString(), null, cancellationToken);
        FamilyFacts facts = (await views.FactsAsync([ask.PersonId], cancellationToken))[ask.PersonId];
        string name = (await people.NamesAsync([ask.PersonId], cancellationToken)).GetValueOrDefault(ask.PersonId) ?? "";
        List<Guid> told = facts.CanSignIn ? [ask.PersonId] : [];
        told.AddRange(facts.Minor ? facts.Parents : []);
        told.Add(ask.AskedBy);
        foreach (Guid person in told.Distinct())
            await notifications.RaiseAsync(new Notification(
                MinistriesNotifications.Answered, NotificationAudience.Person(person),
                person == ask.PersonId ? $"You're in {ministry.Name}" : $"{name} is in {ministry.Name}",
                ask.As == Places.Team ? "A leader said yes to joining the team." : "A leader said yes. See what's on.",
                $"/ministries/{ministry.Id}", Subject: $"ministries/answered/{ask.Id:N}/{person:N}") { Actor = SignedIn(context) }, cancellationToken);
        return new Done();
    }

    public async ValueTask<Done> ChangePersonAsync(PersonChange request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = SignedIn(context);
        MinistryRecord ministry = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
        if (!Roles.IsValid(request.Role))
            throw Invalid("Choose a role.");
        Guid person = Guid.TryParse(request.PersonId, out Guid parsed) ? parsed : throw Invalid("That isn't a person in the app.");
        string key = Roles.KeyOf(request.Role);
        AssignmentOutcome outcome = request.Add
            ? await roles.GiveAsync(me, person, key, ministry.Id.ToString(), null, cancellationToken)
            : await roles.TakeAwayAsync(me, person, key, ministry.Id.ToString(), cancellationToken);
        if (outcome.Refusal is { } refusal)
            throw Refused(refusal);
        if (!request.Add && Roles.KeptAfterTakingOff(request.Role) is { } kept
            && (await roles.GiveAsync(me, person, Roles.KeyOf(kept), ministry.Id.ToString(), null, cancellationToken)).Refusal is { } keptRefusal)
            throw Refused(keptRefusal);
        if (request.Add)
            await db.Asks.Where(ask => ask.MinistryId == ministry.Id && ask.PersonId == person
                && ask.As == (request.Role == Roles.Attendee ? Places.Attending : Places.Team)).ExecuteDeleteAsync(cancellationToken);
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        return new Done();
    }

    public async ValueTask<Done> SetRoleAsync(PersonChange request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = SignedIn(context);
        MinistryRecord ministry = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
        if (request.Role.Length > 0 && !Roles.IsValid(request.Role))
            throw Invalid("Choose a role.");
        Guid person = Guid.TryParse(request.PersonId, out Guid parsed) ? parsed : throw Invalid("That isn't a person in the app.");
        string place = ministry.Id.ToString();
        IReadOnlyList<string> held = await views.RolesAsync(person, ministry.Id, cancellationToken);
        List<string> taking = held.Where(role => role != request.Role).ToList();
        bool giving = request.Role.Length > 0 && !held.Contains(request.Role);
        foreach (string role in taking.Concat(giving ? [request.Role] : []))
            if (!await roles.MayGiveAsync(me, Roles.KeyOf(role), place, cancellationToken))
                throw new RpcException(new Status(StatusCode.PermissionDenied, MinistryRules.WhyNotChangeRole(role)));

        if (giving && (await roles.GiveAsync(me, person, Roles.KeyOf(request.Role), place, null, cancellationToken)).Refusal is { } refusal)
            throw Refused(refusal);
        foreach (string role in taking)
            if ((await roles.TakeAwayAsync(me, person, Roles.KeyOf(role), place, cancellationToken)).Refusal is { } takeRefusal)
                throw Refused(takeRefusal);
        if (request.Role.Length > 0)
            await db.Asks.Where(ask => ask.MinistryId == ministry.Id && ask.PersonId == person
                && ask.As == (request.Role == Roles.Attendee ? Places.Attending : Places.Team)).ExecuteDeleteAsync(cancellationToken);
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        return new Done();
    }

    public async ValueTask<FoundPeople> FindAsync(FindRequest request, CallContext context = default)
    {
        string query = request.Query.Trim();
        if (query.Length < 2)
            return new FoundPeople();
        IReadOnlyList<IndexedPerson> found = await people.FindAsync(query, MostFound, context.CancellationToken);
        return new FoundPeople
        {
            People = found.Where(person => !person.Disabled)
                .Select(person => new FoundPerson { Id = person.Id.ToString(), Name = person.Name, Detail = MaskedEmail(person.Email) })
                .ToList(),
        };
    }

    public async ValueTask<Done> LinkParentAsync(LinkParentRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = SignedIn(context);
        MinistryRecord ministry = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
        Guid child = Guid.TryParse(request.ChildId, out Guid childId) ? childId : throw Invalid("That isn't a person in the app.");
        Guid parent = Guid.TryParse(request.ParentId, out Guid parentId) ? parentId : throw Invalid("Choose their parent.");
        bool here = false;
        foreach (string role in new[] { Roles.Attendee, Roles.Team })
            here |= (await views.HoldersAsync(role, ministry.Id, cancellationToken)).Contains(child);
        if (!here)
            throw Refused($"Only people in {ministry.Name} can be linked from here. Ask the church office.");
        await families.LinkAsync(parent, child, me, cancellationToken);
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        return new Done();
    }

    public async ValueTask<Post> SavePostAsync(PostSave request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = SignedIn(context);
        MinistryRecord ministry = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
        TurnedOn(ministry, MinistryParts.News);
        Post wanted = request.Post;
        if (MinistryRules.WhyNotPost(wanted.Title, wanted.Body, wanted.Audience, MinistryLogic.PostableAudiences(ministry.AttendJoin)) is { } problem)
            throw Invalid(problem);
        DateOnly? sunday = wanted.Sunday.Length == 0 ? null : await SundayOfAsync(ministry, wanted.Sunday, cancellationToken);

        Guid? chosenId = wanted.New ? ParseId(wanted.Id) : null;
        if (chosenId is { } already && await db.Posts.AsNoTracking().SingleOrDefaultAsync(found => found.Id == already, cancellationToken) is { } sent)
            return MinistryViews.Post(sent);
        PostRecord? post = wanted.Id.Length > 0 && !wanted.New
            ? await db.Posts.SingleOrDefaultAsync(found => found.Id == ParseId(wanted.Id) && found.MinistryId == ministry.Id, cancellationToken)
              ?? throw new RpcException(new Status(StatusCode.NotFound, "That post was deleted."))
            : null;
        bool isNew = post is null;
        if (post is null)
        {
            string author = (await people.NamesAsync([me], cancellationToken)).GetValueOrDefault(me) ?? "";
            post = db.Posts.Add(new PostRecord { Id = chosenId ?? Guid.CreateVersion7(), MinistryId = ministry.Id, AuthorId = me, AuthorName = author, PostedAt = Now }).Entity;
        }
        else
            post.EditedAt = Now;
        bool tellNow = wanted.Important && (isNew || !post.Important);
        (post.Title, post.Body, post.Audience, post.Important) = (wanted.Title.Trim(), wanted.Body.Trim(), wanted.Audience, wanted.Important);
        (post.Sunday, post.NoClass) = (sunday, sunday is not null && wanted.NoClass);
        if (wanted.AskWholeChurch && post.WholeChurchAskedAt is null)
            post.WholeChurchAskedAt = Now;
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);

        if (tellNow)
            await notifications.RaiseAsync(new Notification(
                MinistriesNotifications.Important, NotificationAudience.Holding(ImportantReaders(post.Audience), ministry.Id.ToString()),
                $"{ministry.Name}: {post.Title}", Shortened(post.Body), $"/ministries/{ministry.Id}/news",
                Subject: $"ministries/posts/{post.Id:N}", ScopeId: ministry.Id.ToString()) { Actor = me }, cancellationToken);
        return MinistryViews.Post(post);
    }

    private async Task<DateOnly> SundayOfAsync(MinistryRecord group, string wanted, CancellationToken cancellationToken)
    {
        List<SitOutRecord> sitOuts = await db.SitOuts.AsNoTracking().Where(sitOut => sitOut.GroupId == group.Id).ToListAsync(cancellationToken);
        List<string> sundays = group.ShowsInLifeGroups ? MinistryViews.SundaysOf(group, await db.Rounds.AsNoTracking().ToListAsync(cancellationToken), sitOuts) : [];
        return sundays.Contains(wanted) ? DateOf(wanted) : throw Invalid("Choose one of its Sundays.");
    }

    public async ValueTask<Done> DeletePostAsync(ItemRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryRecord ministry = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
        Guid id = ParseId(request.Id);
        await db.Posts.Where(post => post.Id == id && post.MinistryId == ministry.Id).ExecuteDeleteAsync(cancellationToken);
        await notifications.SettleAsync($"ministries/posts/{id:N}", cancellationToken);
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        return new Done();
    }

    public async ValueTask<FileItem> SaveFileAsync(FileSave request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = SignedIn(context);
        MinistryRecord ministry = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
        TurnedOn(ministry, MinistryParts.Files);
        FileItem wanted = request.File;
        if (wanted.Title.Trim().Length is 0 or > MinistryRules.LongestTitle)
            throw Invalid($"Give it a name of up to {MinistryRules.LongestTitle} characters.");
        if (!MinistryLogic.PostableAudiences(ministry.AttendJoin).Contains(wanted.Audience))
            throw Invalid("Choose who it's for.");
        Guid? media = await fileMedia.VerifyAsync(Guid.TryParse(wanted.MediaId, out Guid mediaId) ? mediaId : null, cancellationToken);
        string url = wanted.Url.Trim();
        if (media is null && !IsWebAddress(url))
            throw Invalid("Add a document, or a link that starts with https://.");

        Guid? chosenId = wanted.New ? ParseId(wanted.Id) : null;
        if (chosenId is { } already && await db.Files.AsNoTracking().SingleOrDefaultAsync(found => found.Id == already, cancellationToken) is { } sent)
            return MinistryViews.File(sent);
        FileRecord file = wanted.Id.Length > 0 && !wanted.New
            ? await db.Files.SingleOrDefaultAsync(found => found.Id == ParseId(wanted.Id) && found.MinistryId == ministry.Id, cancellationToken)
              ?? throw new RpcException(new Status(StatusCode.NotFound, "That file was deleted."))
            : db.Files.Add(new FileRecord { Id = chosenId ?? Guid.CreateVersion7(), MinistryId = ministry.Id, AddedBy = me, AddedAt = Now }).Entity;
        if (media is { } kept && kept == file.MediaId && file.Audience != wanted.Audience
            && !await fileMedia.ShareAsync(file.Id, kept, wanted.Audience, ministry.Id, cancellationToken))
            throw Refused("This document is used somewhere else too, so who it's for can't change here. Add it again for the new people.");
        (file.Title, file.Kind, file.Url, file.MediaId, file.Audience, file.Note) =
            (wanted.Title.Trim(), media is null ? FileKinds.Link : FileKinds.Document, media is null ? url : "", media, wanted.Audience, wanted.Note.Trim());
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        await fileMedia.UseAsync(file.Id, media, ministry.Id, ministry.Name, cancellationToken);
        return MinistryViews.File(file);
    }

    public async ValueTask<Done> DeleteFileAsync(ItemRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryRecord ministry = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
        Guid id = ParseId(request.Id);
        await db.Files.Where(file => file.Id == id && file.MinistryId == ministry.Id).ExecuteDeleteAsync(cancellationToken);
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        await fileMedia.ReleaseAsync([id], cancellationToken);
        return new Done();
    }

    public async ValueTask<MinistryDetails> SaveMinistryAsync(MinistrySave request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        Guid id = Existing(tree, request.MinistryId).Id;
        MinistryRecord ministry = await db.Ministries.SingleAsync(found => found.Id == id, cancellationToken);
        MinistryDetails wanted = request.Details;
        await CheckAsync(wanted, cancellationToken);
        Apply(ministry, wanted);
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        return MinistryViews.Details(ministry);
    }

    public async ValueTask<MinistryDetails> SavePartsAsync(PartsSave request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid id = Existing(await views.TreeAsync(cancellationToken), request.MinistryId).Id;
        MinistryRecord ministry = await db.Ministries.SingleAsync(found => found.Id == id, cancellationToken);
        bool calendarWasOn = MinistryViews.IsOn(ministry, MinistryParts.Calendar);
        (ministry.PartsOff, ministry.PartsFromBelow, ministry.UpdatedAt) = (MinistryParts.Valid(request.PartsOff), MinistryParts.ValidFromBelow(request.PartsFromBelow), Now);
        if (request.PartsOrder.Count > 0)
            ministry.PartsOrder = MinistryParts.Ordered(request.PartsOrder);
        if (ChatStarters.IsValid(request.ChatStarters))
            ministry.ChatStarters = request.ChatStarters;
        bool calendarOn = MinistryViews.IsOn(ministry, MinistryParts.Calendar);
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        if (calendarOn != calendarWasOn)
            await calendars.ShowAsync(ministry.Id, calendarOn, MinistryViews.WhileHidden(ministry.Id), cancellationToken);
        return MinistryViews.Details(ministry);
    }

    public async ValueTask<MinistryDetails> AddSubMinistryAsync(MinistrySave request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryRecord parent = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
        return await AddAsync(parent.Id, request.Details, cancellationToken);
    }

    public async ValueTask<MinistryDetails> CreateAsync(MinistrySave request, CallContext context = default) =>
        await AddAsync(null, request.Details, context.CancellationToken);

    public async ValueTask<DeletionPreview> DeletionAsync(MinistryRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        MinistryRecord ministry = Existing(tree, request.MinistryId);
        await MayDeleteAsync(SignedIn(context), ministry, cancellationToken);
        return await remover.PreviewAsync(tree, ministry, cancellationToken);
    }

    public async ValueTask<Done> DeleteAsync(MinistryRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        MinistryRecord ministry = Existing(tree, request.MinistryId);
        await MayDeleteAsync(SignedIn(context), ministry, cancellationToken);
        await remover.RemoveAsync(tree, ministry, cancellationToken);
        return new Done();
    }

    public async ValueTask<Done> ArchiveAsync(MinistryRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryTree every = await views.EveryTreeAsync(cancellationToken);
        MinistryRecord ministry = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
        await MayDeleteAsync(SignedIn(context), ministry, cancellationToken);
        DateTime now = Now;
        foreach (MinistryRecord archiving in MinistryArchive.Archiving(every, ministry))
        {
            await db.Ministries.Where(found => found.Id == archiving.Id).ExecuteUpdateAsync(set => set.SetProperty(found => found.ArchivedAt, now), cancellationToken);
            await calendars.ShowAsync(archiving.Id, false, MinistryViews.WhileHidden(archiving.Id), cancellationToken);
            changes.Touch(archiving.Id, deleted: true);
        }
        await db.SaveChangesAsync(cancellationToken);
        return new Done();
    }

    public async ValueTask<Done> RestoreAsync(MinistryRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryTree every = await views.EveryTreeAsync(cancellationToken);
        MinistryRecord ministry = Existing(every, request.MinistryId);
        await MayDeleteAsync(SignedIn(context), ministry, cancellationToken);
        if (MinistryArchive.WhyNotRestore(every, ministry) is { } why)
            throw Refused(why);
        foreach (MinistryRecord restoring in MinistryArchive.Restoring(every, ministry))
        {
            await db.Ministries.Where(found => found.Id == restoring.Id).ExecuteUpdateAsync(set => set.SetProperty(found => found.ArchivedAt, (DateTime?)null), cancellationToken);
            await calendars.ShowAsync(restoring.Id, MinistryParts.IsOn(restoring.PartsOff, MinistryParts.Calendar), MinistryViews.WhileHidden(restoring.Id), cancellationToken);
            changes.Touch(restoring.Id);
        }
        await db.SaveChangesAsync(cancellationToken);
        return new Done();
    }

    public async ValueTask<ArchivedList> ArchivedAsync(DirectoryRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        GrantSet grants = await views.GrantsAsync(SignedIn(context), cancellationToken);
        MinistryTree every = await views.EveryTreeAsync(cancellationToken);
        return new ArchivedList
        {
            Ministries = every.All.Where(ministry => ministry.ArchivedAt is not null && MayFind(grants, ministry))
                .OrderByDescending(ministry => ministry.ArchivedAt).ThenBy(ministry => ministry.Name, StringComparer.CurrentCulture)
                .Select(ministry => new ArchivedLine
                {
                    Id = ministry.Id.ToString(), Name = ministry.Name, ArchivedAt = ministry.ArchivedAt!.Value, ShowsInLifeGroups = ministry.ShowsInLifeGroups,
                    Where = ministry.ParentId is { } parent && every.Find(parent) is { } above ? $"Under {above.Name}" : "Not under another ministry",
                    MayRestore = MayDelete(grants, ministry) && MinistryArchive.WhyNotRestore(every, ministry) is null,
                })
                .ToList(),
        };
    }

    private static bool MayFind(GrantSet grants, MinistryRecord ministry) =>
        MayDelete(grants, ministry) || MinistryViews.Holds(grants, MinistriesPermissions.Settings, ministry.Id);

    private static bool MayDelete(GrantSet grants, MinistryRecord ministry) =>
        ministry.ParentId is { } parent ? MinistryViews.Holds(grants, MinistriesPermissions.Settings, parent) : grants.IsAllowed(MinistriesPermissions.Create);

    public async ValueTask<MovePreview> MovingAsync(MoveRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        (MinistryRecord ministry, MinistryRecord? under) = await MayMoveAsync(SignedIn(context), tree, request, cancellationToken);
        return await mover.PreviewAsync(tree, ministry, under, cancellationToken);
    }

    public async ValueTask<MinistryDetails> MoveAsync(MoveRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        (MinistryRecord ministry, MinistryRecord? under) = await MayMoveAsync(SignedIn(context), tree, request, cancellationToken);
        return MinistryViews.Details(await mover.MoveAsync(tree, ministry, under, cancellationToken));
    }

    public async ValueTask<CalendarLine> SaveCalendarAsync(CalendarSave request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryRecord ministry = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
        TurnedOn(ministry, MinistryParts.Calendar);
        CalendarLine wanted = request.Calendar;
        if (!MinistryLogic.PostableAudiences(ministry.AttendJoin).Contains(wanted.Audience))
            throw Invalid("Choose who the calendar is for.");
        string name = wanted.Name.Trim().Length > 0 ? wanted.Name : CalendarName(ministry, wanted.Audience);
        MinistryCalendar saved = wanted.Id.Length > 0
            ? await calendars.ChangeAsync(ministry.Id, ParseId(wanted.Id), name, MinistryViews.CalendarAudience(wanted.Audience, ministry.Id), cancellationToken)
            : await calendars.CreateAsync(ministry.Id, name, MinistryViews.CalendarAudience(wanted.Audience, ministry.Id), cancellationToken);
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        return MinistryViews.Calendar(saved);
    }

    public async ValueTask<Done> LinkCalendarAsync(CalendarLinkSave request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid id = Existing(await views.TreeAsync(cancellationToken), request.MinistryId).Id;
        MinistryRecord ministry = await db.Ministries.SingleAsync(found => found.Id == id, cancellationToken);
        Guid calendarId = ParseId(request.CalendarId);
        if (request.Linked)
        {
            TurnedOn(ministry, MinistryParts.Calendar);
            if ((await calendars.ForEveryoneAsync(cancellationToken)).All(calendar => calendar.Id != calendarId || calendar.MinistryId == ministry.Id))
                throw Refused("Only a calendar for everyone, and not one of this ministry's own, can be linked.");
            if (!ministry.LinkedCalendars.Contains(calendarId))
                ministry.LinkedCalendars = [.. ministry.LinkedCalendars, calendarId];
        }
        else
            ministry.LinkedCalendars = ministry.LinkedCalendars.Where(linked => linked != calendarId).ToList();
        ministry.UpdatedAt = Now;
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        return new Done();
    }

    public async ValueTask<LinkableCalendars> LinkableCalendarsAsync(MinistryRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryRecord ministry = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
        return new LinkableCalendars
        {
            Calendars = (await calendars.ForEveryoneAsync(cancellationToken))
                .Where(calendar => calendar.MinistryId != ministry.Id && !ministry.LinkedCalendars.Contains(calendar.Id))
                .Select(calendar => new Crumb { Id = calendar.Id.ToString(), Name = calendar.Name })
                .ToList(),
        };
    }

    public async ValueTask<CalendarRemoval> CalendarRemovalAsync(ItemRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryRecord ministry = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
        MinistryCalendarRemoval removal = await calendars.WhatGoesAsync(ministry.Id, ParseId(request.Id), cancellationToken);
        return new CalendarRemoval { Name = removal.Name, Events = removal.Events + removal.RegularEvents };
    }

    public async ValueTask<Done> RemoveCalendarAsync(ItemRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryRecord ministry = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
        await calendars.RemoveAsync(ministry.Id, ParseId(request.Id), cancellationToken);
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        return new Done();
    }

    public async ValueTask<Round> SaveRoundAsync(RoundSave request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Round wanted = request.Round;
        if (wanted.Name.Trim().Length is 0 or > MinistryRules.LongestName)
            throw Invalid($"Name them in up to {MinistryRules.LongestName} characters.");
        List<DateOnly> sundays = wanted.Sundays.Select(DateOf).Distinct().Order().ToList();
        if (sundays.Count == 0)
            throw Invalid("Choose their Sundays.");
        TimeOnly starts = TimeOf(wanted.Starts, new TimeOnly(9, 0));
        TimeOnly ends = TimeOf(wanted.Ends, new TimeOnly(9, 45));
        if (ends <= starts)
            throw Invalid("A class can't end before it starts.");

        RoundRecord round = wanted.Id.Length > 0
            ? await db.Rounds.SingleOrDefaultAsync(found => found.Id == ParseId(wanted.Id), cancellationToken)
              ?? throw new RpcException(new Status(StatusCode.NotFound, "Those groups' dates were deleted."))
            : db.Rounds.Add(new RoundRecord { Id = Guid.CreateVersion7() }).Entity;
        (round.Name, round.Sundays, round.Starts, round.Ends) = (wanted.Name.Trim(), sundays, starts, ends);
        round.ShowsFrom = wanted.ShowsFrom.Length > 0 ? DateOf(wanted.ShowsFrom) : null;
        changes.Touch(round.Id);
        await db.SaveChangesAsync(cancellationToken);
        await KeepCalendarInStepAsync(cancellationToken);
        return MinistryViews.Round(round);
    }

    public async ValueTask<Done> DeleteRoundAsync(RoundRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid id = ParseId(request.Id);
        if (await db.Ministries.AnyAsync(group => group.RoundId == id, cancellationToken))
            throw Refused("Delete these groups, or move them, first.");
        await db.SitOuts.Where(sitOut => sitOut.RoundId == id).ExecuteDeleteAsync(cancellationToken);
        await db.Rounds.Where(round => round.Id == id).ExecuteDeleteAsync(cancellationToken);
        changes.Touch(id);
        await db.SaveChangesAsync(cancellationToken);
        await KeepCalendarInStepAsync(cancellationToken);
        return new Done();
    }

    public async ValueTask<Done> SitOutAsync(SitOutSave request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        MinistryRecord group = Existing(tree, request.SittingOut.GroupId);
        if (!group.ShowsInLifeGroups || group.RoundId is not null)
            throw Refused($"{group.Name} isn't an all-year Life Group.");
        Guid roundId = ParseId(request.SittingOut.RoundId);
        RoundRecord round = await db.Rounds.AsNoTracking().SingleOrDefaultAsync(found => found.Id == roundId, cancellationToken)
                            ?? throw new RpcException(new Status(StatusCode.NotFound, "Those groups' dates were deleted."));
        string note = request.SittingOut.Note.Trim();
        if (note.Length > MinistryRules.LongestLine)
            throw Invalid($"Keep the note to {MinistryRules.LongestLine} characters.");
        Guid? instead = request.SittingOut.InsteadId.Length > 0 ? Existing(tree, request.SittingOut.InsteadId).Id : null;
        if (instead == group.Id)
            throw Invalid("Choose another group for them to join.");

        SitOutRecord? sitOut = await db.SitOuts.SingleOrDefaultAsync(found => found.GroupId == group.Id && found.RoundId == roundId, cancellationToken);
        bool newlyOff = request.Off && sitOut is null;
        if (!request.Off)
        {
            if (sitOut is not null)
                db.SitOuts.Remove(sitOut);
        }
        else
        {
            sitOut ??= db.SitOuts.Add(new SitOutRecord { GroupId = group.Id, RoundId = roundId, MarkedAt = Now }).Entity;
            (sitOut.Note, sitOut.InsteadId) = (note, instead);
        }
        changes.Touch(group.Id);
        await db.SaveChangesAsync(cancellationToken);

        if (newlyOff && round.Sundays.Count > 0 && round.Sundays.Max() >= ChurchTime.Today(time))
            await notifications.RaiseAsync(new Notification(
                MinistriesNotifications.Important, NotificationAudience.Holding(MinistriesPermissions.AttendeesView, group.Id.ToString()),
                $"{group.Name} isn't on {Span(round)}", note.Length > 0 ? note : "The group is back after these Sundays.",
                $"/ministries/{group.Id}", Subject: $"ministries/sit-out/{group.Id:N}/{roundId:N}", ScopeId: group.Id.ToString()) { Actor = SignedIn(context) }, cancellationToken);
        return new Done();
    }

    public async ValueTask<LifeGroupsCalendar> LifeGroupsCalendarAsync(LifeGroupsRequest request, CallContext context = default) =>
        await CalendarChoiceAsync(UserOf(context), context.CancellationToken);

    public async ValueTask<LifeGroupsCalendar> ShowLifeGroupsInAsync(LifeGroupsCalendarChoice request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        ClaimsPrincipal user = UserOf(context);
        if (request.CalendarId.Length == 0)
            await keptEvents.RemoveAsync(LifeGroupSundays.Keeper, cancellationToken);
        else
            await keptEvents.PutInAsync(user, ParseId(request.CalendarId), LifeGroupSundays.Plan(await db.Rounds.AsNoTracking().ToListAsync(cancellationToken)), cancellationToken);
        return await CalendarChoiceAsync(user, cancellationToken);
    }

    private async Task<LifeGroupsCalendar> CalendarChoiceAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        KeptCalendar? where = await keptEvents.WhereAsync(LifeGroupSundays.Keeper, cancellationToken);
        return new LifeGroupsCalendar
        {
            CalendarId = where?.Id.ToString() ?? "", CalendarName = where?.Name ?? "",
            Choices = (await keptEvents.ChoicesAsync(user, cancellationToken)).Select(choice => new Crumb { Id = choice.Id.ToString(), Name = choice.Name }).ToList(),
        };
    }

    private async Task KeepCalendarInStepAsync(CancellationToken cancellationToken) =>
        await keptEvents.KeepInStepAsync(LifeGroupSundays.Plan(await db.Rounds.AsNoTracking().ToListAsync(cancellationToken)), cancellationToken);

    private static ClaimsPrincipal UserOf(CallContext context) => context.ServerCallContext?.GetHttpContext().User ?? new ClaimsPrincipal();

    private async Task<MinistryDetails> AddAsync(Guid? parentId, MinistryDetails wanted, CancellationToken cancellationToken)
    {
        Guid? chosenId = wanted.New ? ParseId(wanted.Id) : null;
        if (chosenId is { } already && await db.Ministries.AsNoTracking().SingleOrDefaultAsync(found => found.Id == already, cancellationToken) is { } added)
            return MinistryViews.Details(added);
        await CheckAsync(wanted, cancellationToken);
        DateTime now = Now;
        MinistryRecord ministry = new()
        {
            Id = chosenId ?? Guid.CreateVersion7(), ParentId = parentId, CreatedAt = now, PartsOff = MinistryParts.OffWhenNew(),
            Order = await db.Ministries.Where(other => other.ParentId == parentId).Select(other => (int?)other.Order).MaxAsync(cancellationToken) + 1 ?? 0,
        };
        Apply(ministry, wanted);
        db.Ministries.Add(ministry);
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        await roles.ScopesChangedAsync(cancellationToken);
        return MinistryViews.Details(ministry);
    }

    private async Task CheckAsync(MinistryDetails wanted, CancellationToken cancellationToken)
    {
        if (MinistryRules.WhyNotDetails(wanted) is { } problem)
            throw Invalid(problem);
        if (wanted.ShowsInLifeGroups && Guid.TryParse(wanted.RoundId, out Guid round) && !await db.Rounds.AnyAsync(found => found.Id == round, cancellationToken))
            throw Invalid("Those Sundays were deleted. Choose others.");
    }

    private void Apply(MinistryRecord ministry, MinistryDetails wanted)
    {
        ministry.Name = wanted.Name.Trim();
        ministry.Summary = wanted.Summary.Trim();
        ministry.About = wanted.About.Trim();
        ministry.Meets = wanted.Meets.Trim();
        ministry.WhoFor = wanted.WhoFor.Trim();
        ministry.Colour = wanted.Colour.Trim();
        ministry.Icon = wanted.Icon.Trim();
        ministry.TeamJoin = wanted.TeamJoin;
        ministry.AttendJoin = wanted.AttendJoin;
        ministry.ShowsInLifeGroups = wanted.ShowsInLifeGroups;
        ministry.RoundId = wanted.ShowsInLifeGroups && Guid.TryParse(wanted.RoundId, out Guid round) ? round : null;
        ministry.UpdatedAt = Now;
    }

    private static void TurnedOn(MinistryRecord ministry, string part)
    {
        if (!MinistryViews.IsOn(ministry, part))
            throw Refused($"{MinistryParts.Of(part).Name} is turned off for {ministry.Name}. Turn it on in Settings first.");
    }

    private async Task MayDeleteAsync(Guid me, MinistryRecord ministry, CancellationToken cancellationToken)
    {
        if (!MayDelete(await views.GrantsAsync(me, cancellationToken), ministry))
            throw new RpcException(new Status(StatusCode.PermissionDenied, ministry.ParentId is null
                ? "Only staff delete the church's top-level ministries."
                : "Only the leaders of the ministry above this one can delete it."));
    }

    private async Task<(MinistryRecord Ministry, MinistryRecord? Under)> MayMoveAsync(Guid me, MinistryTree tree, MoveRequest request, CancellationToken cancellationToken)
    {
        MinistryRecord ministry = Existing(tree, request.MinistryId);
        MinistryRecord? under = request.UnderId.Length > 0 ? Existing(tree, request.UnderId) : null;
        GrantSet grants = await views.GrantsAsync(me, cancellationToken);
        if (MinistryRules.WhyNotMove(tree, ministry, under, place => MayAddUnder(grants, place)) is { } refusal)
            throw MayAddUnder(grants, under) ? Refused(refusal) : new RpcException(new Status(StatusCode.PermissionDenied, refusal));
        return (ministry, under);
    }

    private static bool MayAddUnder(GrantSet grants, MinistryRecord? place) =>
        place is null ? grants.IsAllowed(MinistriesPermissions.Create) : MinistryViews.Holds(grants, MinistriesPermissions.Settings, place.Id);

    private static Permission ImportantReaders(string audience) => audience switch
    {
        Audiences.Team => MinistriesPermissions.TeamView,
        Audiences.Parents => MinistriesPermissions.ParentsView,
        _ => MinistriesPermissions.AttendeesView,
    };

    private static string CalendarName(MinistryRecord ministry, string audience) => audience switch
    {
        Audiences.Team => $"{ministry.Name} team",
        Audiences.Parents => $"{ministry.Name} parents",
        Audiences.Attendees => $"{ministry.Name} families",
        _ => ministry.Name,
    };

    private static string Span(RoundRecord round) =>
        round.Sundays.Min() == round.Sundays.Max()
            ? $"on {round.Sundays.Min().ToString("d MMMM", CultureInfo.InvariantCulture)}"
            : $"from {round.Sundays.Min().ToString("d MMMM", CultureInfo.InvariantCulture)} to {round.Sundays.Max().ToString("d MMMM", CultureInfo.InvariantCulture)}";

    private static string Shortened(string body) => body.Length <= 140 ? body : body[..137].TrimEnd() + "…";

    private static bool IsWebAddress(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && uri.Scheme == Uri.UriSchemeHttps;

    public static string MaskedEmail(string email)
    {
        int at = email.IndexOf('@', StringComparison.Ordinal);
        return at <= 0 ? "" : $"{email[0]}•••{email[at..]}";
    }

    private static DateOnly DateOf(string text) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date) ? date : throw Invalid("That isn't a date.");

    private static TimeOnly TimeOf(string text, TimeOnly fallback) =>
        text.Length == 0 ? fallback
        : TimeOnly.TryParseExact(text, ["HH:mm", "HH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly value) ? value
        : throw Invalid("That isn't a time.");

    private static Guid ParseId(string id) => Guid.TryParse(id, out Guid parsed) ? parsed : throw Invalid("That isn't here any more.");
}

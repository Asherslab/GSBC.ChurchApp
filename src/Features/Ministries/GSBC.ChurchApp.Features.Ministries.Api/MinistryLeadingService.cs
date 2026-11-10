using System.Globalization;
using Grpc.Core;
using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
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
    INotifications notifications,
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

        List<(Guid Person, string Role, string From)> team = [];
        foreach (string role in new[] { Roles.Leader, Roles.Admin, Roles.Team })
            team.AddRange((await views.HoldersAsync(role, ministry.Id, cancellationToken)).Select(person => (person, role, "")));
        foreach (MinistryRecord above in tree.Above(ministry.Id))
            team.AddRange((await views.HoldersAsync(Roles.Leader, above.Id, cancellationToken)).Select(person => (person, Roles.Leader, above.Name)));
        team = team.GroupBy(member => member.Person).Select(member => member.OrderBy(each => RoleOrder(each.Role)).ThenBy(each => each.From.Length).First()).ToList();
        IReadOnlyList<Guid> attendees = await views.HoldersAsync(Roles.Attendee, ministry.Id, cancellationToken);

        List<Guid> everyone = team.Select(member => member.Person).Concat(attendees).Distinct().ToList();
        IReadOnlyDictionary<Guid, FamilyFacts> facts = await views.FactsAsync(everyone, cancellationToken);
        List<Guid> parents = facts.Values.SelectMany(fact => fact.Parents).Distinct().ToList();
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync([.. everyone, .. parents], cancellationToken);

        Member MemberOf(Guid person, string role, string from)
        {
            FamilyFacts? fact = facts.GetValueOrDefault(person);
            return new Member
            {
                PersonId = person.ToString(), Name = names.GetValueOrDefault(person) ?? "Someone", Role = role, Age = fact?.Minor == true ? fact.Age : null,
                Parents = (fact?.Parents ?? []).Select(parent => new PersonLine { Id = parent.ToString(), Name = names.GetValueOrDefault(parent) ?? "A parent" }).ToList(),
                NoParent = fact is { Minor: true, Parents.Count: 0 }, CanSignIn = fact?.CanSignIn ?? true, LeadsFrom = from,
            };
        }

        List<string> mayGive = [];
        foreach (string role in new[] { Roles.Leader, Roles.Admin, Roles.Team, Roles.Attendee })
            if (await roles.MayGiveAsync(me, Roles.KeyOf(role), ministry.Id.ToString(), cancellationToken))
                mayGive.Add(role);

        List<AskRecord> asks = await db.Asks.AsNoTracking().Where(ask => ask.MinistryId == ministry.Id).OrderBy(ask => ask.AskedAt).ToListAsync(cancellationToken);
        return new PeopleList
        {
            Team = team.Select(member => MemberOf(member.Person, member.Role, member.From))
                .OrderBy(member => RoleOrder(member.Role)).ThenBy(member => member.Name, StringComparer.CurrentCulture).ToList(),
            Attendees = attendees.Select(person => MemberOf(person, Roles.Attendee, ""))
                .OrderByDescending(member => member.NoParent).ThenBy(member => member.Name, StringComparer.CurrentCulture).ToList(),
            Asks = await AskLinesAsync(asks, tree, views, people, cancellationToken),
            MayGive = mayGive,
            Attending = ministry.AttendJoin != Joining.None || attendees.Count > 0,
        };
    }

    public async ValueTask<Done> AnswerAsync(AskRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        MinistryRecord ministry = Existing(tree, request.MinistryId);
        Guid id = Guid.TryParse(request.AskId, out Guid parsed) ? parsed : Guid.Empty;
        AskRecord ask = await db.Asks.SingleOrDefaultAsync(found => found.Id == id && found.MinistryId == ministry.Id, cancellationToken)
                        ?? throw new RpcException(new Status(StatusCode.NotFound, "That ask was already answered or taken back."));
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
                $"/ministries/{ministry.Id}", Subject: $"ministries/answered/{ask.Id:N}/{person:N}"), cancellationToken);
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
        if (request.Add)
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
        if (MinistryRules.WhyNotPost(wanted.Title, wanted.Body, wanted.Audience, MinistryRules.PostableAudiences(ministry.AttendJoin)) is { } problem)
            throw Invalid(problem);

        PostRecord? post = wanted.Id.Length > 0
            ? await db.Posts.SingleOrDefaultAsync(found => found.Id == ParseId(wanted.Id) && found.MinistryId == ministry.Id, cancellationToken)
              ?? throw new RpcException(new Status(StatusCode.NotFound, "That post was deleted."))
            : null;
        bool isNew = post is null;
        if (post is null)
        {
            string author = (await people.NamesAsync([me], cancellationToken)).GetValueOrDefault(me) ?? "";
            post = db.Posts.Add(new PostRecord { Id = Guid.CreateVersion7(), MinistryId = ministry.Id, AuthorId = me, AuthorName = author, PostedAt = Now }).Entity;
        }
        else
            post.EditedAt = Now;
        bool tellNow = wanted.Important && (isNew || !post.Important);
        (post.Title, post.Body, post.Audience, post.Important) = (wanted.Title.Trim(), wanted.Body.Trim(), wanted.Audience, wanted.Important);
        if (wanted.AskWholeChurch && post.WholeChurchAskedAt is null)
            post.WholeChurchAskedAt = Now;
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);

        if (tellNow)
            await notifications.RaiseAsync(new Notification(
                MinistriesNotifications.Important, NotificationAudience.Holding(ImportantReaders(post.Audience), ministry.Id.ToString()),
                $"{ministry.Name}: {post.Title}", Shortened(post.Body), $"/ministries/{ministry.Id}/news",
                Subject: $"ministries/posts/{post.Id:N}", ScopeId: ministry.Id.ToString()), cancellationToken);
        return MinistryViews.Post(post);
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
        if (!MinistryRules.PostableAudiences(ministry.AttendJoin).Contains(wanted.Audience))
            throw Invalid("Choose who it's for.");
        Guid? media = Guid.TryParse(wanted.MediaId, out Guid mediaId) ? mediaId : null;
        string url = wanted.Url.Trim();
        if (media is null && !IsWebAddress(url))
            throw Invalid("Add a document, or a link that starts with https://.");

        FileRecord file = wanted.Id.Length > 0
            ? await db.Files.SingleOrDefaultAsync(found => found.Id == ParseId(wanted.Id) && found.MinistryId == ministry.Id, cancellationToken)
              ?? throw new RpcException(new Status(StatusCode.NotFound, "That file was deleted."))
            : db.Files.Add(new FileRecord { Id = Guid.CreateVersion7(), MinistryId = ministry.Id, AddedBy = me, AddedAt = Now }).Entity;
        (file.Title, file.Kind, file.Url, file.MediaId, file.Audience, file.Note) =
            (wanted.Title.Trim(), media is null ? FileKinds.Link : FileKinds.Document, media is null ? url : "", media, wanted.Audience, wanted.Note.Trim());
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
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
        return new Done();
    }

    public async ValueTask<MinistryDetails> SaveMinistryAsync(MinistrySave request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        Guid id = Existing(tree, request.MinistryId).Id;
        MinistryRecord ministry = await db.Ministries.SingleAsync(found => found.Id == id, cancellationToken);
        MinistryDetails wanted = request.Details;
        wanted.Kind = ministry.Kind;
        if (MinistryRules.WhyNotDetails(wanted) is { } problem)
            throw Invalid(problem);
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
        if (parent.Kind == MinistryKinds.LifeGroup)
            throw Refused("A Life Group can't have ministries under it.");
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
        if (!MinistryRules.PostableAudiences(ministry.AttendJoin).Contains(wanted.Audience))
            throw Invalid("Choose who the calendar is for.");
        string name = wanted.Name.Trim().Length > 0 ? wanted.Name : CalendarName(ministry, wanted.Audience);
        MinistryCalendar saved = wanted.Id.Length > 0
            ? await calendars.ChangeAsync(ministry.Id, ParseId(wanted.Id), name, MinistryViews.CalendarAudience(wanted.Audience, ministry.Id), cancellationToken)
            : await calendars.CreateAsync(ministry.Id, name, MinistryViews.CalendarAudience(wanted.Audience, ministry.Id), cancellationToken);
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        return MinistryViews.Calendar(saved);
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
        MinistryRecord home = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
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
            ? await db.Rounds.SingleOrDefaultAsync(found => found.Id == ParseId(wanted.Id) && found.MinistryId == home.Id, cancellationToken)
              ?? throw new RpcException(new Status(StatusCode.NotFound, "Those groups' dates were deleted."))
            : db.Rounds.Add(new RoundRecord { Id = Guid.CreateVersion7(), MinistryId = home.Id }).Entity;
        (round.Name, round.Sundays, round.Starts, round.Ends) = (wanted.Name.Trim(), sundays, starts, ends);
        round.ShowsFrom = wanted.ShowsFrom.Length > 0 ? DateOf(wanted.ShowsFrom) : null;
        changes.Touch(home.Id);
        await db.SaveChangesAsync(cancellationToken);
        return MinistryViews.Round(round);
    }

    public async ValueTask<Done> DeleteRoundAsync(ItemRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryRecord home = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
        Guid id = ParseId(request.Id);
        if (await db.Ministries.AnyAsync(group => group.RoundId == id, cancellationToken))
            throw Refused("Delete these groups, or move them, first.");
        await db.Rounds.Where(round => round.Id == id && round.MinistryId == home.Id).ExecuteDeleteAsync(cancellationToken);
        changes.Touch(home.Id);
        await db.SaveChangesAsync(cancellationToken);
        return new Done();
    }

    public async ValueTask<Done> SaveSessionAsync(SessionSave request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryRecord group = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
        DateOnly date = DateOf(request.Change.Date);
        string note = request.Change.Note.Trim();
        SessionRecord? session = await db.Sessions.SingleOrDefaultAsync(found => found.GroupId == group.Id && found.Date == date, cancellationToken);
        if (!request.Change.Off && note.Length == 0)
        {
            if (session is not null)
                db.Sessions.Remove(session);
        }
        else
        {
            session ??= db.Sessions.Add(new SessionRecord { GroupId = group.Id, Date = date }).Entity;
            (session.Off, session.Note) = (request.Change.Off, note);
        }
        changes.Touch(group.Id);
        await db.SaveChangesAsync(cancellationToken);
        return new Done();
    }

    private async Task<MinistryDetails> AddAsync(Guid? parentId, MinistryDetails wanted, CancellationToken cancellationToken)
    {
        if (wanted.Kind.Length == 0)
            wanted.Kind = MinistryKinds.Ministry;
        if (MinistryRules.WhyNotDetails(wanted) is { } problem)
            throw Invalid(problem);
        if (Guid.TryParse(wanted.RoundId, out Guid round) && !await db.Rounds.AnyAsync(found => found.Id == round, cancellationToken))
            throw Invalid("Those Sundays were deleted. Choose others.");
        DateTime now = Now;
        MinistryRecord ministry = new()
        {
            Id = Guid.CreateVersion7(), ParentId = parentId, Kind = wanted.Kind, CreatedAt = now,
            Order = await db.Ministries.Where(other => other.ParentId == parentId).Select(other => (int?)other.Order).MaxAsync(cancellationToken) + 1 ?? 0,
        };
        Apply(ministry, wanted);
        db.Ministries.Add(ministry);
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        await roles.ScopesChangedAsync(cancellationToken);
        return MinistryViews.Details(ministry);
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
        ministry.Room = wanted.Room.Trim();
        ministry.JoinAsParent = wanted.JoinAsParent;
        ministry.RoundId = Guid.TryParse(wanted.RoundId, out Guid round) ? round : null;
        ministry.UpdatedAt = Now;
    }

    private static void TurnedOn(MinistryRecord ministry, string part)
    {
        if (!MinistryViews.IsOn(ministry, part))
            throw Refused($"{MinistryParts.Of(part).Name} is turned off for {ministry.Name}. Turn it on in Settings first.");
    }

    private async Task MayDeleteAsync(Guid me, MinistryRecord ministry, CancellationToken cancellationToken)
    {
        GrantSet grants = await views.GrantsAsync(me, cancellationToken);
        bool may = ministry.ParentId is { } parent
            ? MinistryViews.Holds(grants, MinistriesPermissions.Settings, parent)
            : grants.IsAllowed(MinistriesPermissions.Create);
        if (!may)
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

    private static int RoleOrder(string role) => role switch { Roles.Leader => 0, Roles.Admin => 1, _ => 2 };

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
        : TimeOnly.TryParseExact(text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly value) ? value
        : throw Invalid("That isn't a time.");

    private static Guid ParseId(string id) => Guid.TryParse(id, out Guid parsed) ? parsed : throw Invalid("That isn't here any more.");
}

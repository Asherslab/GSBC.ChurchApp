using System.Security.Claims;
using GSBC.ChurchApp.Features.Rosters.Api.Data;
using GSBC.ChurchApp.Features.Rosters.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Api.Ministries;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Rosters.Api;

internal sealed class RostersService(
    RostersDbContext db,
    RosterViews views,
    RosterPublisher publisher,
    RosterMessenger messenger,
    IMinistryFacts ministries,
    IMinistryCalendars calendars,
    IRosterableEvents events,
    IFamilies families,
    IPeopleIndex people,
    IAccessEvaluator access,
    TimeProvider time) : IRostersService
{
    private const int MostPositionNeeds = 20;
    private const int EventChoiceWeeks = 13;

    public ValueTask<RosterView> RosterAsync(RosterQuery request, CallContext context = default) => RosterAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<RosterView> RosterAsync(Guid me, RosterQuery request, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await RosteringAsync(request.MinistryId, cancellationToken);
        RosterBook book = await views.LoadAsync(ministry.Id, cancellationToken, DayOrNull(request.From), DayOrNull(request.To)?.AddDays(1))
                          ?? throw NotHere();
        Recipient? recipient = book.Recipients().GetValueOrDefault(me);
        if ((await access.GrantsForAsync(me, cancellationToken)).IsAllowed(RostersPermissions.Edit, ministry.Id.ToString()))
            return book.ViewFor(me, (recipient ?? new Recipient(true, [])) with { Leads = true });
        return recipient is not null
            ? book.ViewFor(me, recipient)
            : throw new RpcException(new Status(StatusCode.PermissionDenied, $"Only {ministry.Name}'s team sees its roster."));
    }

    public ValueTask<EventChoices> EventChoicesAsync(RosterQuery request, CallContext context = default) => EventChoicesAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<EventChoices> EventChoicesAsync(Guid me, RosterQuery request, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await RosteringAsync(request.MinistryId, cancellationToken);
        DateOnly today = ChurchTime.Today(time);
        DateOnly from = DayOf(request.From) ?? today;
        DateOnly to = DayOf(request.To) ?? today.AddDays(EventChoiceWeeks * 7);
        HashSet<Guid> own = (await calendars.OfAsync(ministry.Id, cancellationToken)).Select(calendar => calendar.Id).ToHashSet();
        DateTime now = time.GetUtcNow().UtcDateTime;
        return new EventChoices
        {
            Events = (await events.VisibleToAsync(me, from, to, cancellationToken))
                .Where(found => found.StartsAt > now)
                .Select(found => new EventChoice
                {
                    Key = found.Key, Title = found.Title, CalendarName = found.CalendarName, StartsAt = found.StartsAt, EndsAt = found.EndsAt,
                    AllDay = found.AllDay, Own = own.Contains(found.CalendarId),
                })
                .ToList(),
        };
    }

    public ValueTask<AwayList> AwayAsync(Nothing request, CallContext context = default) => AwayAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<AwayList> AwayAsync(Guid me, Nothing request, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, FamilyFacts> facts = await families.FactsAsync([me], cancellationToken);
        List<Guid> covered = (facts.GetValueOrDefault(me)?.Children ?? []).Prepend(me).Distinct().ToList();
        IReadOnlyDictionary<Guid, FamilyFacts> coveredFacts = await families.FactsAsync(covered, cancellationToken);
        covered = covered.Where(person => person == me || coveredFacts.GetValueOrDefault(person) is { Minor: true }).ToList();
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(covered, cancellationToken);
        DateOnly today = ChurchTime.Today(time);
        ILookup<Guid, AwayRecord> spans = (await db.Away.AsNoTracking().Where(span => covered.Contains(span.PersonId) && span.To >= today).OrderBy(span => span.From)
            .ToListAsync(cancellationToken)).ToLookup(span => span.PersonId);
        return new AwayList
        {
            People = covered.Select(person => new AwayOf
            {
                PersonId = person.ToString(), Name = names.GetValueOrDefault(person) ?? "Someone",
                Spans = spans[person].Select(span => new AwaySpan { Id = span.Id.ToString(), From = RosterBook.Wire(span.From), To = RosterBook.Wire(span.To), Note = span.Note })
                    .ToList(),
            }).ToList(),
        };
    }

    public ValueTask<Done> SavePositionAsync(PositionSave request, CallContext context = default) => SavePositionAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<Done> SavePositionAsync(Guid me, PositionSave request, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await ChangeableAsync(request.MinistryId, cancellationToken);
        Position sent = request.Position;
        string name = sent.Name.Trim();
        if (name.Length == 0)
            throw Invalid("Give the position a name.");

        Guid id = IdOf(sent.Id, "position");
        PositionRecord? position = await db.Positions.SingleOrDefaultAsync(each => each.Id == id, cancellationToken);
        if (position is not null && position.MinistryId != ministry.Id)
            throw NotHere();
        if (position is null)
        {
            int last = await db.Positions.Where(each => each.MinistryId == ministry.Id).Select(each => (int?)each.Order).MaxAsync(cancellationToken) ?? 0;
            position = new PositionRecord { Id = id, MinistryId = ministry.Id, Order = last + 1, CreatedAt = time.GetUtcNow().UtcDateTime };
            db.Positions.Add(position);
        }

        position.Name = name;
        position.Needed = Math.Clamp(sent.Needed, 1, MostPositionNeeds);
        position.NeedsBlueCard = sent.NeedsBlueCard;
        position.NeedsTraining = sent.NeedsTraining;
        position.NeedsTrained = sent.NeedsTrained;
        await db.Assignments.Where(each => each.PositionId == id).ExecuteUpdateAsync(set => set.SetProperty(each => each.PositionName, name), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(ministry.Id, cancellationToken);
        return new Done();
    }

    public ValueTask<Done> DeletePositionAsync(ItemRef request, CallContext context = default) => DeletePositionAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<Done> DeletePositionAsync(Guid me, ItemRef request, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await ChangeableAsync(request.MinistryId, cancellationToken);
        Guid id = IdOf(request.Id, "position");
        if (await db.Positions.SingleOrDefaultAsync(each => each.Id == id && each.MinistryId == ministry.Id, cancellationToken) is not { } position)
            return new Done();

        DateTime now = time.GetUtcNow().UtcDateTime;
        List<(AssignmentRecord Assignment, DateRecord Date)> coming = await ComingAsync(db.Assignments.Where(each => each.PositionId == id), now, cancellationToken);
        await TakeOffAsync(ministry, coming, $"{position.Name} is no longer on {ministry.Name}'s roster.", me, cancellationToken);
        db.Positions.Remove(position);
        await db.Trained.Where(each => each.PositionId == id).ExecuteDeleteAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(ministry.Id, cancellationToken);
        return new Done();
    }

    public ValueTask<Done> SetTrainedAsync(TrainedChange request, CallContext context = default) => SetTrainedAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<Done> SetTrainedAsync(Guid me, TrainedChange request, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await ChangeableAsync(request.MinistryId, cancellationToken);
        Guid positionId = IdOf(request.PositionId, "position");
        Guid person = IdOf(request.PersonId, "person");
        if (!await db.Positions.AnyAsync(each => each.Id == positionId && each.MinistryId == ministry.Id, cancellationToken))
            throw NotHere();

        TrainedRecord? trained = await db.Trained.SingleOrDefaultAsync(each => each.PositionId == positionId && each.PersonId == person, cancellationToken);
        if (request.Trained && trained is null)
            db.Trained.Add(new TrainedRecord { PositionId = positionId, PersonId = person, AddedBy = me, AddedAt = time.GetUtcNow().UtcDateTime });
        else if (!request.Trained && trained is not null)
            db.Trained.Remove(trained);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(ministry.Id, cancellationToken);
        return new Done();
    }

    public ValueTask<Done> SaveSettingsAsync(SettingsSave request, CallContext context = default) => SaveSettingsAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<Done> SaveSettingsAsync(Guid me, SettingsSave request, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await ChangeableAsync(request.MinistryId, cancellationToken);
        RosterSettings settings = RosterRules.Normalised(request.Settings);
        SettingsRecord? record = await db.Settings.SingleOrDefaultAsync(each => each.MinistryId == ministry.Id, cancellationToken);
        if (record is null)
            db.Settings.Add(record = new SettingsRecord { MinistryId = ministry.Id });
        record.Replies = settings.Replies;
        record.SwapWays = settings.SwapWays;
        record.WhoSees = settings.WhoSees;
        record.RemindDaysBefore = settings.RemindDaysBefore;
        record.NudgeAfterDays = settings.NudgeAfterDays;
        record.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(ministry.Id, cancellationToken);
        return new Done();
    }

    public ValueTask<Done> AddDateAsync(DateAdd request, CallContext context = default) => AddDateAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<Done> AddDateAsync(Guid me, DateAdd request, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await ChangeableAsync(request.MinistryId, cancellationToken);
        Guid id = IdOf(request.DateId, "date");
        if (await db.Dates.AnyAsync(date => date.Id == id || (date.MinistryId == ministry.Id && date.EventKey == request.EventKey), cancellationToken))
            return new Done();
        if ((await events.FindAsync([request.EventKey], cancellationToken)).GetValueOrDefault(request.EventKey) is not { } found)
            throw Refused("That event isn't in the calendar any more.");
        if (RosterRules.WhyNotChange(false, found.StartsAt, time.GetUtcNow().UtcDateTime) is { } why)
            throw Refused(why);

        db.Dates.Add(new DateRecord
        {
            Id = id, MinistryId = ministry.Id, EventKey = found.Key, Title = found.Title, CalendarName = found.CalendarName,
            StartsAt = found.StartsAt, EndsAt = found.EndsAt, AllDay = found.AllDay, NotOn = found.NotOn, AddedBy = me, AddedAt = time.GetUtcNow().UtcDateTime,
        });
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(ministry.Id, cancellationToken);
        return new Done();
    }

    public ValueTask<Done> RemoveDateAsync(ItemRef request, CallContext context = default) => RemoveDateAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<Done> RemoveDateAsync(Guid me, ItemRef request, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await ChangeableAsync(request.MinistryId, cancellationToken);
        Guid id = IdOf(request.Id, "date");
        if (await db.Dates.SingleOrDefaultAsync(date => date.Id == id && date.MinistryId == ministry.Id, cancellationToken) is not { } removed)
            return new Done();

        List<(AssignmentRecord, DateRecord)> coming = await ComingAsync(db.Assignments.Where(each => each.DateId == id), time.GetUtcNow().UtcDateTime, cancellationToken);
        await TakeOffAsync(ministry, coming, $"{ministry.Name} isn't rostering {removed.Title} on {RosterText.Day(removed.StartsAt)} any more.", me, cancellationToken);
        List<Guid> assignments = await db.Assignments.Where(each => each.DateId == id).Select(each => each.Id).ToListAsync(cancellationToken);
        await db.Swaps.Where(swap => assignments.Contains(swap.AssignmentId)).ExecuteDeleteAsync(cancellationToken);
        await db.Assignments.Where(each => each.DateId == id).ExecuteDeleteAsync(cancellationToken);
        db.Dates.Remove(removed);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(ministry.Id, cancellationToken);
        return new Done();
    }

    public ValueTask<Done> AssignAsync(AssignRequest request, CallContext context = default) => AssignAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<Done> AssignAsync(Guid me, AssignRequest request, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await ChangeableAsync(request.MinistryId, cancellationToken);
        Guid id = IdOf(request.AssignmentId, "place");
        if (await db.Assignments.AnyAsync(each => each.Id == id, cancellationToken))
            return new Done();

        Guid dateId = IdOf(request.DateId, "date");
        Guid positionId = IdOf(request.PositionId, "position");
        Guid person = IdOf(request.PersonId, "person");
        DateRecord date = await db.Dates.AsNoTracking().SingleOrDefaultAsync(each => each.Id == dateId && each.MinistryId == ministry.Id, cancellationToken) ?? throw NotHere();
        PositionRecord position = await db.Positions.AsNoTracking().SingleOrDefaultAsync(each => each.Id == positionId && each.MinistryId == ministry.Id, cancellationToken)
                                  ?? throw NotHere();
        DateTime now = time.GetUtcNow().UtcDateTime;
        if (RosterRules.WhyNotChange(ministry.Archived, date.StartsAt, now) is { } why)
            throw Refused(why);
        if ((await ministries.TeamAsync(ministry.Id, cancellationToken)).All(member => member.PersonId != person))
            throw Refused($"Add them to {ministry.Name}'s team first.");
        if (await db.Assignments.AnyAsync(each => each.DateId == dateId && each.PositionId == positionId && each.PersonId == person, cancellationToken))
            return new Done();

        RosterSettings settings = RosterRules.SettingsOf(await db.Settings.AsNoTracking().SingleOrDefaultAsync(each => each.MinistryId == ministry.Id, cancellationToken));
        db.Assignments.Add(new AssignmentRecord
        {
            Id = id, DateId = dateId, MinistryId = ministry.Id, PositionId = positionId, PositionName = position.Name, PersonId = person,
            State = RosterRules.FirstState(settings.Replies), AssignedBy = me, AssignedAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(ministry.Id, cancellationToken);
        await messenger.TellAsync(person, RostersNotifications.PutOn, $"You're on {RosterText.Day(date.StartsAt)}: {position.Name.ToLowerInvariant()}",
            settings.Replies ? $"{RosterText.Line(date, position.Name)}, for {ministry.Name}. Say whether you can do it." : $"{RosterText.Line(date, position.Name)}, for {ministry.Name}.",
            ministry.Id, $"rosters/on/{id:N}", me, cancellationToken, settings.Replies ? "Answer" : "See the roster");
        return new Done();
    }

    public ValueTask<Done> UnassignAsync(ItemRef request, CallContext context = default) => UnassignAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<Done> UnassignAsync(Guid me, ItemRef request, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await ChangeableAsync(request.MinistryId, cancellationToken);
        Guid id = IdOf(request.Id, "place");
        List<(AssignmentRecord Assignment, DateRecord Date)> coming = await ComingAsync(db.Assignments.Where(each => each.Id == id && each.MinistryId == ministry.Id),
            time.GetUtcNow().UtcDateTime, cancellationToken);
        if (!await db.Assignments.AnyAsync(each => each.Id == id && each.MinistryId == ministry.Id, cancellationToken))
            return new Done();

        await TakeOffAsync(ministry, coming.Where(each => Replies.Fills(each.Assignment.State)).ToList(), null, me, cancellationToken);
        await db.Swaps.Where(swap => swap.AssignmentId == id).ExecuteDeleteAsync(cancellationToken);
        await db.Assignments.Where(each => each.Id == id).ExecuteDeleteAsync(cancellationToken);
        await publisher.PublishAsync(ministry.Id, cancellationToken);
        await messenger.SettleAsync($"rosters/on/{id:N}", cancellationToken);
        return new Done();
    }

    public ValueTask<Done> DecideSwapAsync(SwapDecision request, CallContext context = default) => DecideSwapAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<Done> DecideSwapAsync(Guid me, SwapDecision request, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await ChangeableAsync(request.MinistryId, cancellationToken);
        Guid id = IdOf(request.SwapId, "swap");
        SwapRecord swap = await db.Swaps.SingleOrDefaultAsync(each => each.Id == id && each.MinistryId == ministry.Id, cancellationToken) ?? throw NotHere();
        if (swap.State != SwapStates.Offered || swap.OfferedBy is not { } taker)
            throw Refused("This swap has already been settled.");
        AssignmentRecord assignment = await db.Assignments.SingleAsync(each => each.Id == swap.AssignmentId, cancellationToken);
        DateRecord date = await db.Dates.AsNoTracking().SingleAsync(each => each.Id == assignment.DateId, cancellationToken);
        if (RosterRules.WhyNotChange(ministry.Archived, date.StartsAt, time.GetUtcNow().UtcDateTime) is { } why)
            throw Refused(why);

        Guid? was = assignment.PersonId;
        if (request.Yes)
            Move(assignment, swap, taker, me);
        else
            (swap.State, swap.OfferedBy) = (SwapStates.Open, null);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(ministry.Id, cancellationToken);
        await messenger.SettleAsync($"rosters/swap/{id:N}", cancellationToken);

        string line = RosterText.Line(date, assignment.PositionName);
        if (request.Yes)
        {
            await messenger.TellAsync(taker, RostersNotifications.Swaps, $"You're on {RosterText.Day(date.StartsAt)}", $"A leader said yes: {line}.", ministry.Id,
                $"rosters/swapped/{id:N}", me, cancellationToken);
            if (was is { } before)
                await messenger.TellAsync(before, RostersNotifications.Swaps, "Your swap is done", $"Someone else is taking your place: {line}.", ministry.Id,
                    $"rosters/swapped/{id:N}", me, cancellationToken);
        }
        else
        {
            await messenger.TellAsync(taker, RostersNotifications.Swaps, "A leader said no to the swap", $"You're not taking that place: {line}.", ministry.Id,
                $"rosters/swapped/{id:N}", me, cancellationToken);
        }
        return new Done();
    }

    public ValueTask<Done> ReplyAsync(ReplyRequest request, CallContext context = default) => ReplyAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<Done> ReplyAsync(Guid me, ReplyRequest request, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await RosteringAsync(request.MinistryId, cancellationToken);
        Guid id = IdOf(request.AssignmentId, "place");
        AssignmentRecord assignment = await db.Assignments.SingleOrDefaultAsync(each => each.Id == id && each.MinistryId == ministry.Id, cancellationToken) ?? throw NotHere();
        DateRecord date = await db.Dates.AsNoTracking().SingleAsync(each => each.Id == assignment.DateId, cancellationToken);
        RosterSettings settings = RosterRules.SettingsOf(await db.Settings.AsNoTracking().SingleOrDefaultAsync(each => each.MinistryId == ministry.Id, cancellationToken));
        bool mayAct = assignment.PersonId is { } person && await families.MayActForAsync(me, person, cancellationToken);
        if (RosterRules.WhyNotReply(mayAct, ministry.Archived, settings.Replies, date.StartsAt, time.GetUtcNow().UtcDateTime) is { } why)
            throw Refused(why);

        string state = request.Accept ? Replies.Accepted : Replies.Declined;
        if (assignment.State == state && assignment.Reason == request.Reason.Trim())
            return new Done();
        assignment.State = state;
        assignment.Reason = request.Accept ? "" : request.Reason.Trim();
        assignment.RepliedBy = me;
        assignment.RepliedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(ministry.Id, cancellationToken);
        await messenger.SettleAsync($"rosters/on/{id:N}", cancellationToken);

        if (!request.Accept)
        {
            string name = (await people.NamesAsync([assignment.PersonId!.Value], cancellationToken)).GetValueOrDefault(assignment.PersonId.Value) ?? "Someone";
            string reason = assignment.Reason.Length > 0 ? $" They said: “{assignment.Reason}”" : "";
            await messenger.TellLeadersAsync(ministry.Id, RostersNotifications.ForLeaders, $"{RosterRules.First(name)} can't do {RosterText.Day(date.StartsAt)}",
                $"{RosterText.Line(date, assignment.PositionName)}.{reason} Find someone else on the roster.", $"rosters/declined/{id:N}", me, cancellationToken);
        }
        return new Done();
    }

    public ValueTask<Done> AskSwapAsync(SwapAsk request, CallContext context = default) => AskSwapAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<Done> AskSwapAsync(Guid me, SwapAsk request, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await RosteringAsync(request.MinistryId, cancellationToken);
        Guid swapId = IdOf(request.SwapId, "swap");
        if (await db.Swaps.AnyAsync(each => each.Id == swapId, cancellationToken))
            return new Done();

        Guid id = IdOf(request.AssignmentId, "place");
        AssignmentRecord assignment = await db.Assignments.AsNoTracking().SingleOrDefaultAsync(each => each.Id == id && each.MinistryId == ministry.Id, cancellationToken)
                                      ?? throw NotHere();
        DateRecord date = await db.Dates.AsNoTracking().SingleAsync(each => each.Id == assignment.DateId, cancellationToken);
        RosterSettings settings = RosterRules.SettingsOf(await db.Settings.AsNoTracking().SingleOrDefaultAsync(each => each.MinistryId == ministry.Id, cancellationToken));
        IReadOnlyList<MinistryMember> team = await ministries.TeamAsync(ministry.Id, cancellationToken);
        Guid? to = request.Way == SwapWays.Direct && Guid.TryParse(request.ToPersonId, out Guid chosen) ? chosen : null;
        bool mayAct = assignment.PersonId is { } person && await families.MayActForAsync(me, person, cancellationToken);
        bool asked = await db.Swaps.AnyAsync(each => each.AssignmentId == id && (each.State == SwapStates.Open || each.State == SwapStates.Offered), cancellationToken);
        if (RosterRules.WhyNotAskSwap(mayAct, ministry.Archived, settings.SwapWays, request.Way, assignment.State, asked, date.StartsAt, time.GetUtcNow().UtcDateTime,
                to is { } toPerson && toPerson != assignment.PersonId && team.Any(member => member.PersonId == toPerson)) is { } why)
            throw Refused(why);

        db.Swaps.Add(new SwapRecord
        {
            Id = swapId, AssignmentId = id, MinistryId = ministry.Id, Way = request.Way, State = SwapStates.Open, AskedBy = me, AskedTo = to,
            Note = request.Note.Trim(), AskedAt = time.GetUtcNow().UtcDateTime,
        });
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(ministry.Id, cancellationToken);

        string name = RosterRules.First((await people.NamesAsync([assignment.PersonId!.Value], cancellationToken)).GetValueOrDefault(assignment.PersonId.Value) ?? "Someone");
        string line = RosterText.Line(date, assignment.PositionName);
        string note = request.Note.Trim().Length > 0 ? $" “{request.Note.Trim()}”" : "";
        if (to is { } direct)
            await messenger.TellAsync(direct, RostersNotifications.Swaps, $"Can you take {name}'s place?", $"{line}.{note}", ministry.Id, $"rosters/swap/{swapId:N}",
                me, cancellationToken, "Answer");
        else
            foreach (MinistryMember member in team.Where(member => !member.Leads && member.PersonId != assignment.PersonId))
                await messenger.TellAsync(member.PersonId, RostersNotifications.Swaps, $"Can anyone take {name}'s place?", $"{line}.{note}", ministry.Id,
                    $"rosters/swap/{swapId:N}", me, cancellationToken, "See the roster");
        await messenger.TellLeadersAsync(ministry.Id, RostersNotifications.ForLeaders, $"{name} asked for a swap",
            to is null ? $"{line}. The team can pick it up." : $"{line}. They asked someone directly.", $"rosters/swap-asked/{swapId:N}", me, cancellationToken);
        return new Done();
    }

    public ValueTask<Done> TakeSwapAsync(SwapTake request, CallContext context = default) => TakeSwapAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<Done> TakeSwapAsync(Guid me, SwapTake request, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await RosteringAsync(request.MinistryId, cancellationToken);
        Guid id = IdOf(request.SwapId, "swap");
        SwapRecord swap = await db.Swaps.SingleOrDefaultAsync(each => each.Id == id && each.MinistryId == ministry.Id, cancellationToken) ?? throw NotHere();
        Guid taker = request.PersonId.Length > 0 ? IdOf(request.PersonId, "person") : me;
        AssignmentRecord assignment = await db.Assignments.SingleAsync(each => each.Id == swap.AssignmentId, cancellationToken);
        DateRecord date = await db.Dates.AsNoTracking().SingleAsync(each => each.Id == assignment.DateId, cancellationToken);
        bool onTeam = (await ministries.TeamAsync(ministry.Id, cancellationToken)).Any(member => member.PersonId == taker);
        bool mayAct = await families.MayActForAsync(me, taker, cancellationToken);
        if (RosterRules.WhyNotTake(swap, taker, assignment.PersonId, onTeam, mayAct, ministry.Archived, date.StartsAt, time.GetUtcNow().UtcDateTime) is { } why)
            throw Refused(why);

        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync([taker, .. assignment.PersonId is { } was ? new[] { was } : []], cancellationToken);
        string takerName = RosterRules.First(names.GetValueOrDefault(taker) ?? "Someone");
        string line = RosterText.Line(date, assignment.PositionName);
        if (!request.Yes)
        {
            if (swap.Way != SwapWays.Direct)
                return new Done();
            swap.State = SwapStates.Declined;
            swap.SettledAt = time.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(cancellationToken);
            await publisher.PublishAsync(ministry.Id, cancellationToken);
            await messenger.SettleAsync($"rosters/swap/{id:N}", cancellationToken);
            if (assignment.PersonId is { } asker)
                await messenger.TellAsync(asker, RostersNotifications.Swaps, $"{takerName} can't take your place", $"{line}. You're still on; ask someone else or tell a leader.",
                    ministry.Id, $"rosters/swapped/{id:N}", me, cancellationToken);
            return new Done();
        }

        Guid? before = assignment.PersonId;
        string next = RosterRules.AfterTaking(swap.Way);
        if (next == SwapStates.Done)
            Move(assignment, swap, taker, me);
        else
            (swap.State, swap.OfferedBy) = (SwapStates.Offered, taker);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(ministry.Id, cancellationToken);
        await messenger.SettleAsync($"rosters/swap/{id:N}", cancellationToken);

        if (next == SwapStates.Done)
        {
            if (before is { } asker)
                await messenger.TellAsync(asker, RostersNotifications.Swaps, $"{takerName} is taking your place", $"{line}. You're off it.", ministry.Id,
                    $"rosters/swapped/{id:N}", me, cancellationToken);
            await messenger.TellLeadersAsync(ministry.Id, RostersNotifications.ForLeaders, $"{takerName} took a place on the roster", $"{line}.",
                $"rosters/swapped/{id:N}", me, cancellationToken);
        }
        else
        {
            await messenger.TellLeadersAsync(ministry.Id, RostersNotifications.ForLeaders, $"{takerName} offers to take a place", $"{line}. Say yes or no on the roster.",
                $"rosters/swap/{id:N}", me, cancellationToken);
        }
        return new Done();
    }

    public ValueTask<Done> WithdrawSwapAsync(SwapRef request, CallContext context = default) => WithdrawSwapAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<Done> WithdrawSwapAsync(Guid me, SwapRef request, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await RosteringAsync(request.MinistryId, cancellationToken);
        Guid id = IdOf(request.SwapId, "swap");
        if (await db.Swaps.SingleOrDefaultAsync(each => each.Id == id && each.MinistryId == ministry.Id, cancellationToken) is not { } swap || !SwapStates.Live(swap.State))
            return new Done();
        AssignmentRecord assignment = await db.Assignments.AsNoTracking().SingleAsync(each => each.Id == swap.AssignmentId, cancellationToken);
        if (assignment.PersonId is not { } person || !await families.MayActForAsync(me, person, cancellationToken))
            throw Refused("Only they, or a parent of theirs, can call off the swap.");

        db.Swaps.Remove(swap);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(ministry.Id, cancellationToken);
        await messenger.SettleAsync($"rosters/swap/{id:N}", cancellationToken);
        return new Done();
    }

    public ValueTask<Done> SaveAwayAsync(AwaySave request, CallContext context = default) => SaveAwayAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<Done> SaveAwayAsync(Guid me, AwaySave request, CancellationToken cancellationToken)
    {
        Guid person = request.PersonId.Length > 0 ? IdOf(request.PersonId, "person") : me;
        if (!await families.MayActForAsync(me, person, cancellationToken))
            throw Refused("Only they, or a parent of theirs, can say when they're away.");
        if (!DateOnly.TryParseExact(request.From, "yyyy-MM-dd", out DateOnly from) || !DateOnly.TryParseExact(request.To, "yyyy-MM-dd", out DateOnly to))
            throw Invalid("Choose the first and last day.");
        if (RosterRules.WhyNotAway(from, to, ChurchTime.Today(time)) is { } why)
            throw Invalid(why);

        Guid id = IdOf(request.AwayId, "away dates");
        AwayRecord? away = await db.Away.SingleOrDefaultAsync(each => each.Id == id, cancellationToken);
        if (away is not null && away.PersonId != person)
            throw NotHere();
        if (away is null)
            db.Away.Add(away = new AwayRecord { Id = id, PersonId = person });
        (away.From, away.To, away.Note, away.MadeBy, away.MadeAt) = (from, to, request.Note.Trim(), me, time.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAwayAsync([person], cancellationToken);
        await publisher.PublishAllAsync(cancellationToken);
        await TellOfClashesAsync(person, new AwayDays(from, to), me, cancellationToken);
        return new Done();
    }

    public ValueTask<Done> RemoveAwayAsync(AwayRef request, CallContext context = default) => RemoveAwayAsync(SignedIn(context), request, context.CancellationToken);

    internal async ValueTask<Done> RemoveAwayAsync(Guid me, AwayRef request, CancellationToken cancellationToken)
    {
        Guid id = IdOf(request.AwayId, "away dates");
        if (await db.Away.SingleOrDefaultAsync(each => each.Id == id, cancellationToken) is not { } away)
            return new Done();
        if (!await families.MayActForAsync(me, away.PersonId, cancellationToken))
            throw Refused("Only they, or a parent of theirs, can change when they're away.");

        db.Away.Remove(away);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAwayAsync([away.PersonId], cancellationToken);
        await publisher.PublishAllAsync(cancellationToken);
        return new Done();
    }

    private async Task TellOfClashesAsync(Guid person, AwayDays span, Guid actor, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<(AssignmentRecord Assignment, DateRecord Date)> coming = await ComingAsync(db.Assignments.Where(each => each.PersonId == person), now, cancellationToken);
        List<(AssignmentRecord Assignment, DateRecord Date)> clashes = coming
            .Where(each => Replies.Fills(each.Assignment.State) && span.Covers(ChurchTime.DayOf(each.Date.StartsAt))).ToList();
        if (clashes.Count == 0)
            return;

        string name = RosterRules.First((await people.NamesAsync([person], cancellationToken)).GetValueOrDefault(person) ?? "Someone");
        foreach ((AssignmentRecord assignment, DateRecord date) in clashes)
            await messenger.TellLeadersAsync(assignment.MinistryId, RostersNotifications.ForLeaders, $"{name} is away on {RosterText.Day(date.StartsAt)}",
                $"They're on the roster for it: {RosterText.Line(date, assignment.PositionName)}.", $"rosters/clash/{assignment.Id:N}", actor, cancellationToken);
    }

    private async Task<List<(AssignmentRecord Assignment, DateRecord Date)>> ComingAsync(IQueryable<AssignmentRecord> assignments, DateTime now, CancellationToken cancellationToken) =>
        (await assignments.AsNoTracking()
            .Join(db.Dates.AsNoTracking().Where(date => date.StartsAt > now), assignment => assignment.DateId, date => date.Id, (assignment, date) => new { assignment, date })
            .ToListAsync(cancellationToken))
        .Select(each => (each.assignment, each.date)).ToList();

    private async Task TakeOffAsync(MinistryFacts ministry, IReadOnlyList<(AssignmentRecord Assignment, DateRecord Date)> coming, string? why, Guid actor,
        CancellationToken cancellationToken)
    {
        foreach ((AssignmentRecord assignment, DateRecord date) in coming)
            if (assignment.PersonId is { } person)
                await messenger.TellAsync(person, RostersNotifications.PutOn, $"You're off the roster for {RosterText.Day(date.StartsAt)}",
                    why ?? $"A leader took you off: {RosterText.Line(date, assignment.PositionName)}, for {ministry.Name}.", ministry.Id, $"rosters/off/{assignment.Id:N}",
                    actor, cancellationToken);
    }

    private void Move(AssignmentRecord assignment, SwapRecord swap, Guid taker, Guid by)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        (assignment.PersonId, assignment.State, assignment.Reason, assignment.RepliedBy, assignment.RepliedAt) = (taker, Replies.Accepted, "", by, now);
        (assignment.RemindedAt, assignment.NudgedAt) = (null, null);
        (swap.State, swap.OfferedBy, swap.SettledAt) = (SwapStates.Done, taker, now);
    }

    private async Task<MinistryFacts> RosteringAsync(string ministryId, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await ministries.FindAsync(IdOf(ministryId, "ministry"), cancellationToken) ?? throw NotHere();
        return ministry.Uses(RosterParts.Key) ? ministry : throw Refused($"The roster is turned off for {ministry.Name}. Turn it on in Settings first.");
    }

    private async Task<MinistryFacts> ChangeableAsync(string ministryId, CancellationToken cancellationToken)
    {
        MinistryFacts ministry = await RosteringAsync(ministryId, cancellationToken);
        return ministry.Archived ? throw Refused(RosterRules.WhyNotChange(true, DateTime.MaxValue, DateTime.MinValue)!) : ministry;
    }

    private static DateOnly? DayOf(string text) => DateOnly.TryParseExact(text, "yyyy-MM-dd", out DateOnly day) ? day : null;

    private static DateTime? DayOrNull(string text) => DayOf(text) is { } day ? ChurchTime.ToUtc(day.ToDateTime(TimeOnly.MinValue)) : null;

    private static Guid IdOf(string text, string what) => Guid.TryParse(text, out Guid id) ? id : throw Invalid($"That {what} isn't recognised.");

    private static ClaimsPrincipal User(CallContext context) => context.ServerCallContext!.GetHttpContext().User;

    private static Guid SignedIn(CallContext context) =>
        User(context) is { Identity.IsAuthenticated: true } user && Caller.UserId(user) is { } id
            ? id
            : throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in again."));

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private static RpcException Refused(string message) => new(new Status(StatusCode.FailedPrecondition, message));

    private static RpcException NotHere() => new(new Status(StatusCode.NotFound, "That isn't on the roster any more."));
}

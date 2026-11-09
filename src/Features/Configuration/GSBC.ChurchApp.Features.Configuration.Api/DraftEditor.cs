using System.Security.Claims;
using System.Text.Json;
using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using static GSBC.ChurchApp.Features.Configuration.Api.PlacementEditor;

namespace GSBC.ChurchApp.Features.Configuration.Api;

internal sealed class DraftEditor(
    ConfigurationDbContext db, PlacementEditor placements, PlacementRules rules, DraftRules draftRules, SurfaceCatalog surfaces, IAccessEvaluator access,
    IRoleAssignments roles, IMediaReferences media, INotifications notifications, TimeProvider time)
{
    private const int RecentlyPublished = 10;

    public async Task<DraftList> ListAsync(ClaimsPrincipal caller, CancellationToken cancellationToken)
    {
        GrantSet grants = await GrantsAsync(caller, cancellationToken);
        List<DraftRecord> drafts = await db.Drafts.AsNoTracking().OrderByDescending(draft => draft.UpdatedAt).ToListAsync(cancellationToken);
        List<DraftRecord> visible = drafts.Where(draft => CanSee(grants, draft)).ToList();
        List<Guid> ids = visible.Select(draft => draft.Id).ToList();
        List<DraftChangeRecord> changes = await db.DraftChanges.AsNoTracking().Where(change => ids.Contains(change.DraftId)).ToListAsync(cancellationToken);

        List<Draft> messages = [];
        foreach (DraftRecord draft in visible)
            messages.Add(await ToMessageAsync(draft, changes.Where(change => change.DraftId == draft.Id), caller, cancellationToken));

        return new DraftList
        {
            Drafts = messages,
            Published = await RecentlyPublishedAsync(cancellationToken),
            SeesEveryDraft = SeesEvery(grants),
        };
    }

    public async Task<Draft> GetAsync(ClaimsPrincipal caller, DraftQuery query, CancellationToken cancellationToken)
    {
        DraftRecord draft = await VisibleAsync(caller, query.DraftId, cancellationToken);
        return await ToMessageAsync(draft, await ChangesOfAsync(draft, cancellationToken), caller, cancellationToken);
    }

    public async Task<Draft> CreateAsync(ClaimsPrincipal caller, NewDraftRequest request, CancellationToken cancellationToken)
    {
        Guid author = CallerId(caller);
        GrantSet grants = await GrantsAsync(caller, cancellationToken);
        if (!surfaces.Areas.Values.Any(area => MayArrange(grants, area)))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "You can't arrange anything in the app, so you can't start a draft."));

        DateTime now = Now;
        DraftRecord draft = new()
        {
            Id = Guid.CreateVersion7(),
            Name = NameOf(request.Name),
            AuthorId = author,
            CreatedAt = now,
            UpdatedAt = now,
            Revision = 1,
        };
        db.Drafts.Add(draft);

        if (request.CopyOf is { } copyOf)
        {
            DraftRecord original = await VisibleAsync(caller, copyOf, cancellationToken);
            foreach (DraftChangeRecord change in await ChangesOfAsync(original, cancellationToken))
            {
                db.DraftChanges.Add(new DraftChangeRecord
                {
                    Id = Guid.CreateVersion7(),
                    DraftId = draft.Id,
                    Area = change.Area,
                    ScopeId = change.ScopeId,
                    PlacementId = change.PlacementId,
                    Surface = change.Surface,
                    Kind = change.Kind,
                    BasedOnRevision = change.BasedOnRevision,
                    Patch = change.Patch,
                    ChangedAt = now,
                    ChangedBy = author,
                });
            }
        }

        await SaveAsync(draft, cancellationToken);
        await roles.GiveAsFeatureAsync(author, ConfigurationPermissions.DraftReviewerRole, draft.Id.ToString(), null, cancellationToken);
        await UseMediaAsync(draft, cancellationToken);
        return await ToMessageAsync(draft, await ChangesOfAsync(draft, cancellationToken), caller, cancellationToken);
    }

    public async Task<Draft> RenameAsync(ClaimsPrincipal caller, RenameDraftRequest request, CancellationToken cancellationToken)
    {
        DraftRecord draft = await ManagedAsync(caller, request.DraftId, cancellationToken);
        draft.Name = NameOf(request.Name);
        Touch(draft);
        await SaveAsync(draft, cancellationToken);
        return await ToMessageAsync(draft, await ChangesOfAsync(draft, cancellationToken), caller, cancellationToken);
    }

    public async Task<Draft> SaveAsync(ClaimsPrincipal caller, SaveDraftRequest request, CancellationToken cancellationToken)
    {
        DraftRecord draft = await ManagedAsync(caller, request.DraftId, cancellationToken);
        Guid actor = CallerId(caller);
        List<DraftChangeRecord> existing = await db.DraftChanges.Where(change => change.DraftId == draft.Id).ToListAsync(cancellationToken);
        List<DraftChangeRecord> touched = [];

        foreach (PatchMessage message in request.Set)
        {
            (SurfaceArea area, string? scopeId) = await placements.AuthoriseAsync(caller, message.Area, message.ScopeId, cancellationToken, publishing: GoesOutToEveryone(draft));
            Surface surface = rules.SurfaceIn(area, message.Surface);
            Guid placementId = ParseId(message.PlacementId);
            DraftChangeRecord? row = existing.SingleOrDefault(change => change.PlacementId == placementId);
            PlacementPatch patch = await ValidatedAsync(area, scopeId, surface, placementId, message, row, cancellationToken);

            if (row is null)
            {
                row = new DraftChangeRecord
                {
                    Id = Guid.CreateVersion7(),
                    DraftId = draft.Id,
                    Area = area.Key,
                    ScopeId = scopeId,
                    PlacementId = placementId,
                    Surface = surface.Key,
                    Patch = "",
                };
                db.DraftChanges.Add(row);
                existing.Add(row);
            }
            row.Kind = patch.Kind;
            row.BasedOnRevision = patch.Base?.Revision ?? 0;
            row.Patch = Serialize(patch);
            row.ChangedAt = Now;
            row.ChangedBy = actor;
            touched.Add(row);
        }

        foreach (string cleared in request.Cleared)
        {
            Guid placementId = ParseId(cleared);
            if (existing.SingleOrDefault(change => change.PlacementId == placementId) is { } row)
            {
                await placements.AuthoriseAsync(caller, row.Area, row.ScopeId, cancellationToken, publishing: GoesOutToEveryone(draft));
                db.DraftChanges.Remove(row);
                existing.Remove(row);
            }
        }

        foreach (IGrouping<(string Area, string? ScopeId), DraftChangeRecord> inArea in touched
                     .Where(row => row.Kind != PatchKind.Remove && existing.Contains(row) && draftRules.HasUniqueNames(row.Area))
                     .GroupBy(row => (row.Area, row.ScopeId)))
        {
            SurfaceArea area = surfaces.Areas[inArea.Key.Area];
            List<StoredPlacement> live = (await placements.InArea(area, inArea.Key.ScopeId).AsNoTracking().ToListAsync(cancellationToken)).Select(Stored).ToList();
            IEnumerable<PlacementPatch> patches = existing.Where(row => row.Area == area.Key && row.ScopeId == inArea.Key.ScopeId).Select(Deserialize);
            draftRules.RefuseClashingNames(area, DraftComposition.Apply(live, patches), inArea.Select(row => row.PlacementId.ToString()).ToList());
        }

        draft.Status = DraftStatus.Open;
        draft.AskedAt = null;
        Touch(draft);
        await SaveAsync(draft, cancellationToken, existing);
        await UseMediaAsync(draft, cancellationToken);
        return await ToMessageAsync(draft, existing, caller, cancellationToken);
    }

    public async Task<Draft> SettleAsync(ClaimsPrincipal caller, SettleRequest request, CancellationToken cancellationToken)
    {
        DraftRecord draft = await ManagedAsync(caller, request.DraftId, cancellationToken);
        Guid placementId = ParseId(request.PlacementId);
        List<DraftChangeRecord> existing = await db.DraftChanges.Where(change => change.DraftId == draft.Id).ToListAsync(cancellationToken);
        DraftChangeRecord row = existing.SingleOrDefault(change => change.PlacementId == placementId)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "That change isn't in this draft any more."));
        await placements.AuthoriseAsync(caller, row.Area, row.ScopeId, cancellationToken, publishing: GoesOutToEveryone(draft));

        PlacementPatch patch = Deserialize(row);
        PlacementRecord? live = await db.Placements.AsNoTracking().SingleOrDefaultAsync(placement => placement.Id == placementId, cancellationToken);
        if (live is null)
        {
            db.DraftChanges.Remove(row);
            existing.Remove(row);
        }
        else
        {
            StoredPlacement now = Stored(live);
            if (DraftRules.Settled(patch, now, request.KeepMine) is not { } settled)
            {
                db.DraftChanges.Remove(row);
                existing.Remove(row);
            }
            else
            {
                row.Patch = Serialize(settled);
                row.BasedOnRevision = now.Revision;
            }
        }

        Touch(draft);
        await SaveAsync(draft, cancellationToken, existing);
        return await ToMessageAsync(draft, existing, caller, cancellationToken);
    }

    public async Task<Draft> AskToPublishAsync(ClaimsPrincipal caller, DraftQuery query, CancellationToken cancellationToken)
    {
        DraftRecord draft = await ManagedAsync(caller, query.DraftId, cancellationToken);
        List<DraftChangeRecord> changes = await ChangesOfAsync(draft, cancellationToken);
        if (changes.Count == 0)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "There's nothing in this draft to publish yet."));

        draft.Status = DraftStatus.WaitingToPublish;
        draft.AskedAt = Now;
        Touch(draft);
        await SaveAsync(draft, cancellationToken, changes);
        if (draftRules.WaitingToPublish(draft, changes, NameOf(caller)) is { } waiting)
            await notifications.RaiseAsync(waiting, cancellationToken);
        return await ToMessageAsync(draft, changes, caller, cancellationToken);
    }

    public async Task<PublishReply> PublishAsync(ClaimsPrincipal caller, DraftQuery query, CancellationToken cancellationToken)
    {
        DraftRecord draft = await VisibleAsync(caller, query.DraftId, cancellationToken, tracked: true);
        Guid batchId = await PublishAsync(draft, CallerId(caller),
            (area, scopeId) => placements.AuthoriseAsync(caller, area, scopeId, cancellationToken, publishing: true), cancellationToken);
        return new PublishReply { BatchId = batchId.ToString() };
    }

    public async Task<Draft> SchedulePublishAsync(ClaimsPrincipal caller, SchedulePublishRequest request, CancellationToken cancellationToken)
    {
        DraftRecord draft = await VisibleAsync(caller, request.DraftId, cancellationToken, tracked: true);
        DateTime at = DateTime.SpecifyKind(request.At, DateTimeKind.Utc);
        if (at <= Now)
            throw Invalid("That time has already passed. Choose a later one, or publish it now.");

        List<DraftChangeRecord> rows = await ChangesOfAsync(draft, cancellationToken);
        await CheckPublishableAsync(rows, (area, scopeId) => placements.AuthoriseAsync(caller, area, scopeId, cancellationToken, publishing: true), cancellationToken);

        draft.PublishAt = at;
        draft.PublishScheduledBy = CallerId(caller);
        draft.NotPublished = null;
        Touch(draft);
        await SaveAsync(draft, cancellationToken, rows);
        return await ToMessageAsync(draft, rows, caller, cancellationToken);
    }

    public async Task<Draft> CancelScheduledPublishAsync(ClaimsPrincipal caller, DraftQuery query, CancellationToken cancellationToken)
    {
        DraftRecord draft = await ManagedAsync(caller, query.DraftId, cancellationToken);
        List<DraftChangeRecord> rows = await ChangesOfAsync(draft, cancellationToken);
        if (draft.PublishAt is null && draft.NotPublished is null)
            return await ToMessageAsync(draft, rows, caller, cancellationToken);

        draft.PublishAt = null;
        draft.PublishScheduledBy = null;
        draft.NotPublished = null;
        Touch(draft);
        await SaveAsync(draft, cancellationToken, rows);
        return await ToMessageAsync(draft, rows, caller, cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduledOutcome>> PublishDueAsync(CancellationToken cancellationToken)
    {
        DateTime now = Now;
        List<Guid> due = await db.Drafts.AsNoTracking()
            .Where(draft => draft.PublishAt != null && draft.PublishAt <= now && draft.NotPublished == null)
            .OrderBy(draft => draft.PublishAt)
            .Select(draft => draft.Id)
            .ToListAsync(cancellationToken);

        List<ScheduledOutcome> outcomes = [];
        foreach (Guid id in due)
        {
            db.ChangeTracker.Clear();
            if (await db.Drafts.SingleOrDefaultAsync(found => found.Id == id && found.PublishAt != null && found.PublishAt <= now && found.NotPublished == null, cancellationToken) is not { } draft)
                continue;
            outcomes.Add(await PublishScheduledAsync(draft, cancellationToken));
        }
        return outcomes;
    }

    private async Task<ScheduledOutcome> PublishScheduledAsync(DraftRecord draft, CancellationToken cancellationToken)
    {
        Guid publisher = draft.PublishScheduledBy ?? draft.AuthorId;
        GrantSet grants = await access.GrantsForAsync(publisher, cancellationToken);
        try
        {
            await PublishAsync(draft, publisher, (area, scopeId) => Task.FromResult(AuthoriseScheduled(grants, area, scopeId)), cancellationToken);
            return new ScheduledOutcome(draft.Id, draft.Name, Published: true);
        }
        catch (RpcException exception) when (exception.StatusCode is StatusCode.Aborted)
        {
            return new ScheduledOutcome(draft.Id, draft.Name, Published: false);
        }
        catch (RpcException exception)
        {
            db.ChangeTracker.Clear();
            if (await db.Drafts.SingleOrDefaultAsync(found => found.Id == draft.Id, cancellationToken) is not { } kept)
                return new ScheduledOutcome(draft.Id, draft.Name, Published: false);
            kept.NotPublished = DraftRules.NotPublished(exception.Status.Detail);
            Touch(kept);
            await SaveAsync(kept, cancellationToken);
            await notifications.RaiseAsync(DraftRules.DidNotPublish(kept, publisher), cancellationToken);
            return new ScheduledOutcome(draft.Id, draft.Name, Published: false, kept.NotPublished);
        }
    }

    public sealed record ScheduledOutcome(Guid DraftId, string Name, bool Published, string? NotPublished = null);

    private (SurfaceArea Area, string? ScopeId) AuthoriseScheduled(GrantSet grants, string areaKey, string? scopeId)
    {
        (SurfaceArea area, string? scope, Permission needed) = rules.Required(areaKey, scopeId, publishing: true);
        return grants.IsAllowed(needed, scope)
            ? (area, scope)
            : throw new RpcException(new Status(StatusCode.PermissionDenied, $"The person who scheduled it can no longer publish {area.Title}."));
    }

    private async Task CheckPublishableAsync(List<DraftChangeRecord> rows, Authorise authorise, CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "There's nothing in this draft to publish yet."));
        foreach (IGrouping<(string Area, string? ScopeId), DraftChangeRecord> inArea in rows.GroupBy(row => (row.Area, row.ScopeId)))
        {
            (SurfaceArea area, string? scopeId) = await authorise(inArea.Key.Area, inArea.Key.ScopeId);
            List<StoredPlacement> live = (await placements.InArea(area, scopeId).AsNoTracking().ToListAsync(cancellationToken)).Select(Stored).ToList();
            draftRules.RefuseChangedForEveryone(live, inArea.Select(Deserialize).ToList());
        }
    }

    private delegate Task<(SurfaceArea Area, string? ScopeId)> Authorise(string area, string? scopeId);

    private async Task<Guid> PublishAsync(DraftRecord draft, Guid actor, Authorise authorise, CancellationToken cancellationToken)
    {
        List<DraftChangeRecord> rows = await ChangesOfAsync(draft, cancellationToken);
        if (rows.Count == 0)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "There's nothing in this draft to publish yet."));

        List<Change> commits = [];
        List<Guid> removed = [];
        foreach (IGrouping<(string Area, string? ScopeId), DraftChangeRecord> inArea in rows.GroupBy(row => (row.Area, row.ScopeId)))
        {
            (SurfaceArea area, string? scopeId) = await authorise(inArea.Key.Area, inArea.Key.ScopeId);
            List<PlacementRecord> existing = await placements.InArea(area, scopeId).ToListAsync(cancellationToken);
            List<PlacementPatch> patches = inArea.Select(Deserialize).ToList();
            draftRules.RefuseChangedForEveryone(existing.Select(Stored), patches);
            draftRules.RefuseClashingNames(area, DraftComposition.Apply(existing.Select(Stored), patches),
                patches.Where(patch => patch.Kind != PatchKind.Remove).Select(patch => patch.PlacementId).ToList());

            foreach (PlacementPatch patch in patches)
            {
                Surface surface = rules.SurfaceIn(area, patch.Surface);
                Guid placementId = ParseId(patch.PlacementId);
                PlacementRecord? record = existing.SingleOrDefault(placement => placement.Id == placementId);
                if (patch.Kind == PatchKind.Remove)
                {
                    if (record is null)
                        continue;
                    commits.Add(new Change(record, PlacementState.Of(record), null));
                    removed.Add(record.Id);
                    continue;
                }
                if (TabBarSurfaces.IsHome(surface))
                    continue;

                StoredPlacement wanted = record is null ? patch.Created() : patch.ApplyTo(Stored(record));
                PlacementState after = await rules.ValidatedAsync(surface, PlacementState.Of(wanted), cancellationToken);
                record ??= placements.Create(area, scopeId, surface, placementId, existing);
                commits.Add(new Change(record, record.Revision == 0 ? null : PlacementState.Of(record), after));
            }
        }

        bool wasLayer = draft.LayerOn;
        Close(draft);
        Guid batchId = await placements.CommitAsync(actor, commits, rollsBack: null, draft, cancellationToken);
        if (wasLayer)
            await RecordLayerAsync(draft, on: false, actor, cancellationToken);
        await AfterClosingAsync(draft, cancellationToken);
        await DropRemovedFromLayerAsync(removed, cancellationToken);
        return batchId;
    }

    public async Task<Draft> TurnOnAsync(ClaimsPrincipal caller, EventLayerRequest request, CancellationToken cancellationToken)
    {
        DraftRecord draft = await VisibleAsync(caller, request.DraftId, cancellationToken, tracked: true);
        List<DraftChangeRecord> rows = await ChangesOfAsync(draft, cancellationToken);
        if (rows.Count == 0)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "There's nothing in this draft to turn on yet."));
        if (request is { StartsAt: { } starts, EndsAt: { } ends } && ends <= starts)
            throw Invalid("The event layer has to end after it starts.");
        if (request.EndsAt is { } end && end <= Now)
            throw Invalid("The event layer's end has already passed.");

        foreach (IGrouping<(string Area, string? ScopeId), DraftChangeRecord> inArea in rows.GroupBy(row => (row.Area, row.ScopeId)))
        {
            (SurfaceArea area, string? scopeId) = await placements.AuthoriseAsync(caller, inArea.Key.Area, inArea.Key.ScopeId, cancellationToken, publishing: true);
            Dictionary<Guid, PlacementRecord> existing = await placements.InArea(area, scopeId).AsNoTracking().ToDictionaryAsync(placement => placement.Id, cancellationToken);
            foreach (PlacementPatch patch in inArea.Select(Deserialize))
            {
                Surface surface = rules.SurfaceIn(area, patch.Surface);
                if (patch.Kind == PatchKind.Remove || TabBarSurfaces.IsHome(surface))
                    continue;
                StoredPlacement wanted = existing.TryGetValue(ParseId(patch.PlacementId), out PlacementRecord? live)
                    ? patch.ApplyTo(Stored(live))
                    : patch.Kind == PatchKind.Add
                        ? patch.Created()
                        : throw new RpcException(new Status(StatusCode.FailedPrecondition, $"{draftRules.TitleOf(patch)} was removed for everyone. Take it out of this draft first."));
                await rules.ValidatedAsync(surface, PlacementState.Of(wanted), cancellationToken);
            }
        }

        Guid actor = CallerId(caller);
        if (await db.Drafts.SingleOrDefaultAsync(other => other.LayerOn && other.Id != draft.Id, cancellationToken) is { } replaced)
        {
            replaced.LayerOn = false;
            Touch(replaced);
            db.FeedChanges.Add(DraftFeedChange(replaced, await ChangesOfAsync(replaced, cancellationToken)));
            await RecordLayerAsync(replaced, on: false, actor, cancellationToken, save: false);
            await db.SaveChangesAsync(cancellationToken);
        }

        draft.LayerOn = true;
        draft.LayerStartsAt = AsUtc(request.StartsAt);
        draft.LayerEndsAt = AsUtc(request.EndsAt);
        draft.DroppedFromLayer = [];
        Touch(draft);
        await SaveAsync(draft, cancellationToken, rows);
        await RecordLayerAsync(draft, on: true, actor, cancellationToken);
        return await ToMessageAsync(draft, rows, caller, cancellationToken);
    }

    public async Task<DraftsDone> TurnOffAsync(ClaimsPrincipal caller, DraftQuery query, CancellationToken cancellationToken)
    {
        DraftRecord draft = await VisibleAsync(caller, query.DraftId, cancellationToken, tracked: true);
        if (!draft.LayerOn)
            return new DraftsDone();

        List<DraftChangeRecord> rows = await ChangesOfAsync(draft, cancellationToken);
        foreach ((string area, string? scopeId) in rows.Select(row => (row.Area, row.ScopeId)).Distinct())
            await placements.AuthoriseAsync(caller, area, scopeId, cancellationToken, publishing: true);

        draft.LayerOn = false;
        Touch(draft);
        db.FeedChanges.Add(LayerFeedChange(null));
        await SaveAsync(draft, cancellationToken, rows);
        await RecordLayerAsync(draft, on: false, CallerId(caller), cancellationToken);
        return new DraftsDone();
    }

    public async Task<Draft> ShareAsync(ClaimsPrincipal caller, ShareRequest request, bool sharing, CancellationToken cancellationToken)
    {
        DraftRecord draft = await ManagedAsync(caller, request.DraftId, cancellationToken);
        Guid person = ParseId(request.UserId);
        if (person == draft.AuthorId)
            throw Invalid("They wrote this draft, so they always see it.");

        if (sharing)
        {
            await roles.GiveAsFeatureAsync(person, ConfigurationPermissions.DraftReviewerRole, draft.Id.ToString(), null, cancellationToken);
            await notifications.RaiseAsync(DraftRules.SharedWith(draft, person, NameOf(caller)), cancellationToken);
        }
        else
            await roles.TakeAwayAsFeatureAsync(person, ConfigurationPermissions.DraftReviewerRole, draft.Id.ToString(), cancellationToken);

        List<DraftChangeRecord> changes = await ChangesOfAsync(draft, cancellationToken);
        Touch(draft);
        await SaveAsync(draft, cancellationToken, changes);
        return await ToMessageAsync(draft, changes, caller, cancellationToken);
    }

    public async Task<DraftsDone> DeleteAsync(ClaimsPrincipal caller, DraftQuery query, CancellationToken cancellationToken)
    {
        DraftRecord draft = await ManagedAsync(caller, query.DraftId, cancellationToken);
        if (draft.LayerOn)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "This draft is on for an event. Turn it off before deleting it."));

        Close(draft);
        await db.SaveChangesAsync(cancellationToken);
        await AfterClosingAsync(draft, cancellationToken);
        return new DraftsDone();
    }

    private async Task<PlacementPatch> ValidatedAsync(
        SurfaceArea area, string? scopeId, Surface surface, Guid placementId, PatchMessage message, DraftChangeRecord? row, CancellationToken cancellationToken)
    {
        PlacementRecord? live = await db.Placements.AsNoTracking()
            .SingleOrDefaultAsync(placement => placement.Id == placementId && placement.Area == area.Key && placement.ScopeId == scopeId, cancellationToken);
        PatchKind kind = Enum.IsDefined((PatchKind)message.Kind) ? (PatchKind)message.Kind : throw Invalid("That isn't a kind of change.");

        if (kind == PatchKind.Add)
        {
            if (live is not null)
                throw Invalid($"{surface.Title} is already placed; change it instead.");
            PlacementRules.RefuseSecondCopy(surface, await placements.InArea(area, scopeId).Select(placement => placement.Surface).ToListAsync(cancellationToken));
        }
        else if (live is null)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"{surface.Title} was removed for everyone, so it can't be changed in this draft."));
        }

        StoredPlacement? basis = row is not null ? Deserialize(row).Base : live is null ? null : Stored(live);
        PlacementPatch patch = message.ToPatch() with
        {
            PlacementId = placementId.ToString(),
            Area = area.Key,
            ScopeId = scopeId,
            Surface = surface.Key,
            Kind = kind,
            Base = basis,
            StartsAt = AsUtc(message.StartsAt),
            EndsAt = AsUtc(message.EndsAt),
        };
        if (kind == PatchKind.Remove)
            return patch;

        StoredPlacement wanted = kind == PatchKind.Add ? patch.Created() : patch.ApplyTo(Stored(live!));
        PlacementState validated = await rules.ValidatedAsync(surface, PlacementState.Of(wanted), cancellationToken);
        return patch with
        {
            Audience = patch.Audience is null ? null : validated.Audience,
            Values = patch.ValueChanges.ToDictionary(pair => pair.Key, pair => pair.Value is null ? null : validated.Values.GetValueOrDefault(pair.Key)),
        };
    }

    private async Task DropRemovedFromLayerAsync(List<Guid> removed, CancellationToken cancellationToken)
    {
        if (removed.Count == 0 || await db.Drafts.SingleOrDefaultAsync(draft => draft.LayerOn, cancellationToken) is not { } layer)
            return;

        List<DraftChangeRecord> rows = await ChangesOfAsync(layer, cancellationToken, tracked: true);
        List<DraftChangeRecord> dropped = rows.Where(row => row.Kind != PatchKind.Add && removed.Contains(row.PlacementId)).ToList();
        if (dropped.Count == 0)
            return;

        layer.DroppedFromLayer = [.. layer.DroppedFromLayer, .. dropped.Select(row => draftRules.TitleOf(Deserialize(row)))];
        db.DraftChanges.RemoveRange(dropped);
        rows.RemoveAll(dropped.Contains);
        layer.Revision++;
        layer.UpdatedAt = Now;
        await SaveAsync(layer, cancellationToken, rows);
    }

    private async Task RecordLayerAsync(DraftRecord draft, bool on, Guid actor, CancellationToken cancellationToken, bool save = true)
    {
        string state = JsonSerializer.Serialize(new { draft.Name, draft.LayerStartsAt, draft.LayerEndsAt }, JsonSerializerOptions.Default);
        db.Changes.Add(new ChangeRecord
        {
            Id = Guid.CreateVersion7(),
            BatchId = Guid.CreateVersion7(),
            Area = EventLayerHistory,
            PlacementId = draft.Id,
            Surface = EventLayerHistory,
            Before = on ? null : state,
            After = on ? state : null,
            At = Now,
            By = actor,
            DraftId = draft.Id,
            DraftName = draft.Name,
        });
        if (save)
            await db.SaveChangesAsync(cancellationToken);
    }

    private void Close(DraftRecord draft)
    {
        if (draft.LayerOn)
            db.FeedChanges.Add(LayerFeedChange(null));
        db.FeedChanges.Add(new FeedChangeRecord
        {
            Feed = ConfigurationFeeds.Drafts,
            RecordId = draft.Id.ToString(),
            Version = draft.Revision + 1,
            Deleted = true,
            ScopeId = draft.Id.ToString(),
        });
        db.Drafts.Remove(draft);
    }

    private async Task AfterClosingAsync(DraftRecord draft, CancellationToken cancellationToken)
    {
        await media.UseAsync(MediaOwner(draft), [], cancellationToken);
        await notifications.SettleAsync(DraftRules.Subject(draft), cancellationToken);
        foreach (RoleHolder holder in await roles.HoldersAsync(ConfigurationPermissions.DraftReviewerRole, draft.Id.ToString(), cancellationToken))
            await roles.TakeAwayAsFeatureAsync(holder.UserId, ConfigurationPermissions.DraftReviewerRole, draft.Id.ToString(), cancellationToken);
    }

    private async Task UseMediaAsync(DraftRecord draft, CancellationToken cancellationToken)
    {
        List<MediaRef> used = rules.MediaOfDraft((await ChangesOfAsync(draft, cancellationToken)).Select(Deserialize));
        await media.UseAsync(MediaOwner(draft), used, cancellationToken);
    }

    private async Task SaveAsync(DraftRecord draft, CancellationToken cancellationToken, List<DraftChangeRecord>? changes = null)
    {
        changes ??= await ChangesOfAsync(draft, cancellationToken);
        db.FeedChanges.Add(DraftFeedChange(draft, changes));
        if (draft.LayerOn)
            db.FeedChanges.Add(LayerFeedChange(LayerOf(draft, changes)));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new RpcException(new Status(StatusCode.Aborted, "This draft was changed somewhere else at the same moment. Reload it and try again."));
        }
        catch (DbUpdateException exception) when (exception.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            throw new RpcException(new Status(StatusCode.Aborted, "Another draft was turned on for an event at the same moment. Reload and try again."));
        }
    }

    private FeedChangeRecord DraftFeedChange(DraftRecord draft, IEnumerable<DraftChangeRecord> changes) => new()
    {
        Feed = ConfigurationFeeds.Drafts,
        RecordId = draft.Id.ToString(),
        Version = draft.Revision,
        Payload = Encode(FeedMessage(draft, changes)),
        SortKey = draft.UpdatedAt.Ticks.ToString("D19", System.Globalization.CultureInfo.InvariantCulture),
        ScopeId = draft.Id.ToString(),
    };

    private FeedChangeRecord LayerFeedChange(EventLayerMessage? layer) => new()
    {
        Feed = ConfigurationFeeds.Placements,
        RecordId = ConfigurationFeeds.EventLayerRecord,
        Version = Now.Ticks,
        Deleted = layer is null,
        Payload = layer is null ? [] : Encode(layer),
        GroupKey = ConfigurationFeeds.EventLayerRecord,
    };

    private static EventLayerMessage LayerOf(DraftRecord draft, IEnumerable<DraftChangeRecord> changes) => new()
    {
        DraftId = draft.Id.ToString(),
        Name = draft.Name,
        StartsAt = draft.LayerStartsAt,
        EndsAt = draft.LayerEndsAt,
        Changes = changes.Select(change => Deserialize(change).ToMessage()).ToList(),
    };

    private static Draft FeedMessage(DraftRecord draft, IEnumerable<DraftChangeRecord> changes) => new()
    {
        Id = draft.Id.ToString(),
        Name = draft.Name,
        AuthorId = draft.AuthorId.ToString(),
        CreatedAt = draft.CreatedAt,
        UpdatedAt = draft.UpdatedAt,
        Status = draft.Status,
        LayerOn = draft.LayerOn,
        LayerStartsAt = draft.LayerStartsAt,
        LayerEndsAt = draft.LayerEndsAt,
        Revision = draft.Revision,
        Changes = changes.OrderBy(change => change.Id).Select(change => Deserialize(change).ToMessage()).ToList(),
        DroppedFromLayer = [.. draft.DroppedFromLayer],
        PublishAt = draft.PublishAt,
        PublishScheduledBy = draft.PublishScheduledBy?.ToString(),
        NotPublished = draft.NotPublished,
    };

    private async Task<Draft> ToMessageAsync(DraftRecord draft, IEnumerable<DraftChangeRecord> changes, ClaimsPrincipal caller, CancellationToken cancellationToken)
    {
        Draft message = FeedMessage(draft, changes);
        message.Mine = draft.AuthorId == CallerId(caller);
        message.ToCheck = await ToCheckAsync(message.Patches(), cancellationToken);
        message.SharedWith = (await roles.HoldersAsync(ConfigurationPermissions.DraftReviewerRole, draft.Id.ToString(), cancellationToken))
            .Where(holder => holder.UserId != draft.AuthorId)
            .Select(holder => holder.UserId.ToString())
            .ToList();
        return message;
    }

    private async Task<int> ToCheckAsync(IReadOnlyList<PlacementPatch> patches, CancellationToken cancellationToken)
    {
        List<Guid> ids = patches.Where(patch => patch.Kind != PatchKind.Add).Select(patch => ParseId(patch.PlacementId)).ToList();
        if (ids.Count == 0)
            return 0;
        List<PlacementRecord> live = await db.Placements.AsNoTracking().Where(placement => ids.Contains(placement.Id)).ToListAsync(cancellationToken);
        return DraftComposition.ChangedSince(live.Select(Stored), patches).Count;
    }

    private async Task<List<PublishedDraft>> RecentlyPublishedAsync(CancellationToken cancellationToken)
    {
        List<ChangeRecord> rows = await db.Changes.AsNoTracking()
            .Where(change => change.DraftId != null && change.Area != EventLayerHistory && change.RollsBack == null)
            .OrderByDescending(change => change.At)
            .Take(RecentlyPublished * 20)
            .ToListAsync(cancellationToken);
        List<PublishedDraft> published = rows
            .GroupBy(change => change.BatchId)
            .Take(RecentlyPublished)
            .Select(batch => new PublishedDraft
            {
                BatchId = batch.Key.ToString(),
                Name = batch.First().DraftName ?? "",
                At = batch.First().At,
                By = batch.First().By.ToString(),
                Areas = batch.Select(change => surfaces.Areas.GetValueOrDefault(change.Area)?.Title ?? change.Area).Distinct().ToList(),
                Changes = batch.Count(),
            })
            .ToList();
        await MarkUndoneAsync(published, rows, cancellationToken);
        return published;
    }

    private async Task MarkUndoneAsync(List<PublishedDraft> published, List<ChangeRecord> rows, CancellationToken cancellationToken)
    {
        List<Rollback> undos = await RollbacksOfAsync(published.Select(entry => Guid.Parse(entry.BatchId)).ToList(), cancellationToken);
        List<Rollback> redos = await RollbacksOfAsync(undos.Select(undo => undo.BatchId).ToList(), cancellationToken);
        List<Guid> ids = rows.Select(change => change.PlacementId).Distinct().ToList();
        Dictionary<Guid, PlacementState> live = await db.Placements.AsNoTracking()
            .Where(placement => ids.Contains(placement.Id))
            .ToDictionaryAsync(placement => placement.Id, PlacementState.Of, cancellationToken);
        PublishHistory.MarkUndone(published, rows, undos, redos, live);
    }

    private async Task<List<Rollback>> RollbacksOfAsync(List<Guid> batchIds, CancellationToken cancellationToken)
    {
        List<Guid?> ids = batchIds.Select(id => (Guid?)id).ToList();
        return await db.Changes.AsNoTracking()
            .Where(change => ids.Contains(change.RollsBack))
            .Select(change => new Rollback(change.BatchId, change.RollsBack!.Value, change.At, change.By))
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<DraftRecord> VisibleAsync(ClaimsPrincipal caller, string id, CancellationToken cancellationToken, bool tracked = false)
    {
        Guid draftId = ParseId(id);
        IQueryable<DraftRecord> drafts = tracked ? db.Drafts : db.Drafts.AsNoTracking();
        DraftRecord? draft = await drafts.SingleOrDefaultAsync(found => found.Id == draftId, cancellationToken);
        if (draft is null || !CanSee(await GrantsAsync(caller, cancellationToken), draft))
            throw new RpcException(new Status(StatusCode.NotFound, "That draft isn't there any more."));
        return draft;
    }

    private async Task<DraftRecord> ManagedAsync(ClaimsPrincipal caller, string id, CancellationToken cancellationToken)
    {
        DraftRecord draft = await VisibleAsync(caller, id, cancellationToken, tracked: true);
        if (draft.AuthorId != CallerId(caller) && !SeesEvery(await GrantsAsync(caller, cancellationToken)))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Only the person who wrote this draft, or someone who publishes, can change it."));
        return draft;
    }

    private Task<List<DraftChangeRecord>> ChangesOfAsync(DraftRecord draft, CancellationToken cancellationToken, bool tracked = false) =>
        (tracked ? db.DraftChanges : db.DraftChanges.AsNoTracking()).Where(change => change.DraftId == draft.Id).ToListAsync(cancellationToken);

    private async Task<GrantSet> GrantsAsync(ClaimsPrincipal caller, CancellationToken cancellationToken) =>
        await access.GrantsForAsync(CallerId(caller), cancellationToken);

    private static bool SeesEvery(GrantSet grants) => grants.IsAllowed(ConfigurationPermissions.PreviewDraft);

    private static bool CanSee(GrantSet grants, DraftRecord draft) =>
        SeesEvery(grants) || grants.IsAllowed(ConfigurationPermissions.PreviewDraft, draft.Id.ToString());

    private static bool MayArrange(GrantSet grants, SurfaceArea area) =>
        area.ScopeKind == ScopeKind.Global
            ? grants.IsAllowed(area.EditPermission)
            : grants.Grants.Any(grant => !grant.Pattern.IsDeny && grant.Pattern.Matches(area.EditPermission.Node));

    private static bool GoesOutToEveryone(DraftRecord draft) => draft.LayerOn || draft is { PublishAt: not null, NotPublished: null };

    private void Touch(DraftRecord draft)
    {
        draft.Revision++;
        draft.UpdatedAt = Now;
    }

    private static StoredPlacement Stored(PlacementRecord record) => PlacementState.Of(record).ToStored(record.Id, record.Revision);

    private static string Serialize(PlacementPatch patch) => JsonSerializer.Serialize(patch.ToMessage(), JsonSerializerOptions.Default);

    private static PlacementPatch Deserialize(DraftChangeRecord row) =>
        (JsonSerializer.Deserialize<PatchMessage>(row.Patch, JsonSerializerOptions.Default) ?? throw new InvalidOperationException("A draft change has no patch.")).ToPatch();

    private static string MediaOwner(DraftRecord draft) => $"config.draft/{draft.Id}";

    private static string NameOf(string name)
    {
        string trimmed = name.Trim();
        return trimmed.Length switch
        {
            0 => throw Invalid("Give the draft a name."),
            > ConfigurationDbContext.MaximumDraftName => throw Invalid($"Keep the name under {ConfigurationDbContext.MaximumDraftName} characters."),
            _ => trimmed,
        };
    }

    private static DateTime? AsUtc(DateTime? value) => value is { } utc ? DateTime.SpecifyKind(utc, DateTimeKind.Utc) : null;

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private static string NameOf(ClaimsPrincipal caller) =>
        caller.FindFirstValue("name") is { Length: > 0 } name ? name : "Someone";
}

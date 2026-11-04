using System.Globalization;
using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GSBC.ChurchApp.Features.Configuration.Api;

internal sealed class PlacementEditor(
    ConfigurationDbContext db, SurfaceCatalog surfaces, PermissionCatalog permissions, IAccessEvaluator access, IMediaReferences media, TimeProvider time)
{
    public const string EventLayerHistory = "event-layer";
    private const int HistoryBatches = 50;

    public async Task<AreaPlacements> GetAreaAsync(ClaimsPrincipal caller, AreaQuery query, CancellationToken cancellationToken)
    {
        (SurfaceArea area, string? scopeId) = await AuthoriseAsync(caller, query.Area, query.ScopeId, cancellationToken);
        return await PlacementsAsync(area, scopeId, cancellationToken);
    }

    public async Task<ChangeList> ListChangesAsync(ClaimsPrincipal caller, AreaQuery query, CancellationToken cancellationToken)
    {
        (SurfaceArea area, string? scopeId) = await AuthoriseAsync(caller, query.Area, query.ScopeId, cancellationToken);
        IQueryable<ChangeRecord> inArea = db.Changes.AsNoTracking().Where(change => change.Area == area.Key && change.ScopeId == scopeId);
        List<Guid> batchIds = await inArea
            .GroupBy(change => change.BatchId)
            .OrderByDescending(batch => batch.Max(change => change.At))
            .Select(batch => batch.Key)
            .Take(HistoryBatches)
            .ToListAsync(cancellationToken);
        List<ChangeRecord> rows = await inArea.Where(change => batchIds.Contains(change.BatchId)).ToListAsync(cancellationToken);

        return new ChangeList
        {
            Batches = rows
                .GroupBy(change => change.BatchId)
                .OrderByDescending(batch => batch.Max(change => change.At))
                .Select(batch => new ChangeBatch
                {
                    Id = batch.Key.ToString(),
                    At = batch.First().At,
                    By = batch.First().By.ToString(),
                    RollsBack = batch.First().RollsBack?.ToString(),
                    DraftName = batch.First().DraftName,
                    Changes = batch.Select(change => new PlacementChange
                    {
                        PlacementId = change.PlacementId.ToString(),
                        Surface = change.Surface,
                        Before = PlacementState.FromJson(change.Before)?.ToMessage(change.PlacementId, change.Area, change.ScopeId),
                        After = PlacementState.FromJson(change.After)?.ToMessage(change.PlacementId, change.Area, change.ScopeId),
                    }).ToList(),
                })
                .ToList(),
        };
    }

    public async Task<AreaPlacements> RollBackAsync(ClaimsPrincipal caller, RollBackRequest request, CancellationToken cancellationToken)
    {
        Guid batchId = ParseId(request.BatchId);
        List<ChangeRecord> batch = await db.Changes.AsNoTracking()
            .Where(change => change.BatchId == batchId && change.Area != EventLayerHistory)
            .ToListAsync(cancellationToken);
        if (batch.Count == 0)
            throw new RpcException(new Status(StatusCode.NotFound, "That change isn't in the history."));

        List<Change> changes = [];
        foreach (IGrouping<(string Area, string? ScopeId), ChangeRecord> inArea in batch.GroupBy(change => (change.Area, change.ScopeId)))
        {
            (SurfaceArea area, string? scopeId) = await AuthoriseAsync(caller, inArea.Key.Area, inArea.Key.ScopeId, cancellationToken, publishing: true);
            List<PlacementRecord> existing = await InArea(area, scopeId).ToListAsync(cancellationToken);
            foreach (ChangeRecord change in inArea)
            {
                PlacementState? target = PlacementState.FromJson(change.Before);
                PlacementRecord? record = existing.SingleOrDefault(placement => placement.Id == change.PlacementId);
                if (target is null)
                {
                    if (record is not null)
                        changes.Add(new Change(record, PlacementState.Of(record), null));
                    continue;
                }

                Surface surface = surfaces.ByKey.TryGetValue(target.Surface, out Surface? known) && known.Area.Key == area.Key
                    ? known
                    : throw new RpcException(new Status(StatusCode.FailedPrecondition, $"'{target.Surface}' isn't in this version of the app any more."));
                PlacementState after = await ValidatedAsync(surface, target, cancellationToken);
                record ??= Create(area, scopeId, surface, change.PlacementId, existing);
                changes.Add(new Change(record, record.Revision == 0 ? null : PlacementState.Of(record), after));
            }
        }

        await CommitAsync(CallerId(caller), changes, rollsBack: batchId, draft: null, cancellationToken);
        return new AreaPlacements();
    }

    public async Task<(SurfaceArea Area, string? ScopeId)> AuthoriseAsync(
        ClaimsPrincipal caller, string areaKey, string? requestedScope, CancellationToken cancellationToken, bool publishing = false)
    {
        if (!surfaces.Areas.TryGetValue(areaKey, out SurfaceArea? area))
            throw new RpcException(new Status(StatusCode.NotFound, $"There's no area '{areaKey}' to arrange."));

        string? scopeId = string.IsNullOrWhiteSpace(requestedScope) ? null : requestedScope.Trim();
        if (area.ScopeKind == ScopeKind.Global && scopeId is not null)
            throw Invalid($"{area.Title} is arranged once for everyone, so it doesn't take a scope.");
        if (area.ScopeKind != ScopeKind.Global && scopeId is null)
            throw Invalid($"Choose which {area.ScopeKind.Name} to arrange.");

        Permission needed = publishing ? area.PublishPermission : area.EditPermission;
        if (!await access.IsAllowedAsync(caller, needed.Node, scopeId, cancellationToken))
            throw new RpcException(new Status(StatusCode.PermissionDenied,
                publishing ? $"You can't put changes to {area.Title} out to everyone. Ask someone who can publish it." : $"You can't arrange {area.Title}."));

        return (area, scopeId);
    }

    public async Task<PlacementState> ValidatedAsync(Surface surface, PlacementState state, CancellationToken cancellationToken)
    {
        PlacementState claimed = Validated(surface, state);
        List<(Setting Setting, IHoldsMedia Holder)> holders = MediaHolders(surface);
        if (holders.Count == 0)
            return claimed;

        List<MediaRef> refs = holders.SelectMany(field => field.Holder.MediaIn(claimed.Values.GetValueOrDefault(field.Setting.Key, ""))).ToList();
        Dictionary<string, MediaRef> verified = (await media.VerifyAsync(refs, cancellationToken)).DistinctBy(item => item.Id).ToDictionary(item => item.Id);
        SortedDictionary<string, string> values = new(claimed.Values);
        foreach ((Setting setting, IHoldsMedia holder) in holders)
        {
            if (values.TryGetValue(setting.Key, out string? text))
                values[setting.Key] = holder.WithMedia(text, verified);
        }
        return Validated(surface, claimed with { Values = values });
    }

    private static List<(Setting Setting, IHoldsMedia Holder)> MediaHolders(Surface surface) =>
        surface.Settings.OfType<IHoldsMedia>().Select(holder => ((Setting)holder, holder)).ToList();

    public IReadOnlyList<MediaRef> MediaOf(string surfaceKey, PlacementState? state) =>
        state is null || !surfaces.ByKey.TryGetValue(surfaceKey, out Surface? surface)
            ? []
            : MediaHolders(surface).SelectMany(field => field.Holder.MediaIn(state.Values.GetValueOrDefault(field.Setting.Key, ""))).ToList();

    private bool HoldsMedia(string surfaceKey) =>
        surfaces.ByKey.TryGetValue(surfaceKey, out Surface? surface) && MediaHolders(surface).Count > 0;

    private PlacementState Validated(Surface surface, PlacementState state)
    {
        state = Normalised(surface, state);
        List<string> problems = [.. surface.ProblemsWith(state.Values)];
        if (!Audience.TryParse(state.Audience, out Audience? audience))
            problems.Add($"'{state.Audience}' isn't an audience.");
        else if (audience is { Node: { } node } && !permissions.ByNode.ContainsKey(node))
            problems.Add($"No permission '{node}' exists to choose an audience by.");
        if (state is { StartsAt: { } starts, EndsAt: { } ends } && ends <= starts)
            problems.Add($"'{surface.Title}' has to end after it starts.");

        return problems.Count == 0 ? state with { Audience = audience!.Text } : throw Invalid(string.Join(" ", problems));
    }

    private static PlacementState Normalised(Surface surface, PlacementState state)
    {
        SortedDictionary<string, string> values = new(state.Values);
        foreach (Setting setting in surface.Settings.Where(setting => setting is INormalises && values.ContainsKey(setting.Key)))
            values[setting.Key] = ((INormalises)setting).Normalise(values[setting.Key]);
        return state with { Values = values };
    }

    public PlacementRecord Create(SurfaceArea area, string? scopeId, Surface surface, Guid id, List<PlacementRecord> existing)
    {
        if (!surface.Repeatable && existing.Any(placement => placement.Surface == surface.Key))
            throw Conflict(surface.Title);

        PlacementRecord record = new()
        {
            Id = id,
            Area = area.Key,
            ScopeId = scopeId,
            Surface = surface.Key,
            IsSingleton = !surface.Repeatable,
        };
        db.Placements.Add(record);
        existing.Add(record);
        return record;
    }

    public async Task<Guid> CommitAsync(Guid actor, List<Change> changes, Guid? rollsBack, DraftRecord? draft, CancellationToken cancellationToken)
    {
        Guid batchId = Guid.CreateVersion7();
        DateTime now = time.GetUtcNow().UtcDateTime;

        foreach ((PlacementRecord record, PlacementState? before, PlacementState? after) in changes)
        {
            if (before is not null && after is not null && before.ToJson() == after.ToJson())
                continue;

            if (after is null)
            {
                db.Placements.Remove(record);
            }
            else
            {
                after.ApplyTo(record);
                record.Revision++;
                record.UpdatedAt = now;
                record.UpdatedBy = actor;
            }

            db.Changes.Add(new ChangeRecord
            {
                Id = Guid.CreateVersion7(),
                BatchId = batchId,
                Area = record.Area,
                ScopeId = record.ScopeId,
                PlacementId = record.Id,
                Surface = record.Surface,
                Before = before?.ToJson(),
                After = after?.ToJson(),
                At = now,
                By = actor,
                RollsBack = rollsBack,
                DraftId = draft?.Id,
                DraftName = draft?.Name,
            });
            db.FeedChanges.Add(FeedChangeOf(record, deleted: after is null));
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw Conflict("it");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw Conflict("it");
        }

        foreach ((PlacementRecord record, _, PlacementState? after) in changes.Where(change => HoldsMedia(change.Record.Surface)))
            await media.UseAsync(MediaOwner(record.Id), MediaOf(record.Surface, after), cancellationToken);
        return batchId;
    }

    private static string MediaOwner(Guid placementId) => $"config.placement/{placementId}";

    public static FeedChangeRecord FeedChangeOf(PlacementRecord record, bool deleted) => new()
    {
        Feed = ConfigurationFeeds.Placements,
        RecordId = record.Id.ToString(),
        Version = deleted ? record.Revision + 1 : record.Revision,
        Deleted = deleted,
        Payload = deleted ? [] : Encode(ToMessage(record)),
        SortKey = record.Order.ToString("D10", CultureInfo.InvariantCulture),
        GroupKey = ConfigurationFeeds.GroupOf(record.Area, record.ScopeId),
        ScopeId = record.ScopeId,
    };

    public static byte[] Encode<T>(T message)
    {
        using MemoryStream stream = new();
        ConfigurationContractsModel.Instance.Serialize(stream, message);
        return stream.ToArray();
    }

    private async Task<AreaPlacements> PlacementsAsync(SurfaceArea area, string? scopeId, CancellationToken cancellationToken) => new()
    {
        Placements = (await InArea(area, scopeId).AsNoTracking().OrderBy(placement => placement.Order).ToListAsync(cancellationToken))
            .Select(ToMessage)
            .ToList(),
    };

    public IQueryable<PlacementRecord> InArea(SurfaceArea area, string? scopeId) =>
        db.Placements.Where(placement => placement.Area == area.Key && placement.ScopeId == scopeId);

    public Surface SurfaceIn(SurfaceArea area, string key) =>
        surfaces.ByKey.TryGetValue(key, out Surface? surface) && surface.Area.Key == area.Key
            ? surface
            : throw Invalid($"'{key}' can't be placed in {area.Title}.");

    public static Placement ToMessage(PlacementRecord record)
    {
        Placement message = PlacementState.Of(record).ToMessage(record.Id, record.Area, record.ScopeId);
        message.Revision = record.Revision;
        message.UpdatedAt = record.UpdatedAt;
        message.UpdatedBy = record.UpdatedBy.ToString();
        return message;
    }

    public static Guid CallerId(ClaimsPrincipal caller) =>
        Caller.UserId(caller) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in again."));

    public static Guid ParseId(string id) =>
        Guid.TryParse(id, out Guid parsed) ? parsed : throw Invalid("That isn't a valid id.");

    public static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    public static RpcException Conflict(string what) =>
        new(new Status(StatusCode.Aborted, $"Someone else changed {what} while you were editing. Reload to see their change."));

    public sealed record Change(PlacementRecord Record, PlacementState? Before, PlacementState? After);
}

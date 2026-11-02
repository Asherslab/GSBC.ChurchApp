using System.Globalization;
using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GSBC.ChurchApp.Features.Configuration.Api;

internal sealed class PlacementEditor(
    ConfigurationDbContext db, SurfaceCatalog surfaces, PermissionCatalog permissions, IAccessEvaluator access, TimeProvider time)
{
    private const int HistoryBatches = 50;

    public async Task<AreaPlacements> GetAreaAsync(ClaimsPrincipal caller, AreaQuery query, CancellationToken cancellationToken)
    {
        (SurfaceArea area, string? scopeId) = await AuthoriseAsync(caller, query.Area, query.ScopeId, cancellationToken);
        return await PlacementsAsync(area, scopeId, cancellationToken);
    }

    public async Task<AreaPlacements> SaveAsync(ClaimsPrincipal caller, SaveRequest request, CancellationToken cancellationToken)
    {
        (SurfaceArea area, string? scopeId) = await AuthoriseAsync(caller, request.Area, request.ScopeId, cancellationToken);
        List<PlacementRecord> existing = await InArea(area, scopeId).ToListAsync(cancellationToken);
        List<Change> changes = [];

        foreach (Placement placement in request.Placements)
        {
            Surface surface = SurfaceIn(area, placement.Surface);
            PlacementState after = Validated(surface, PlacementState.Of(placement));
            PlacementRecord record = string.IsNullOrEmpty(placement.Id)
                ? Create(area, scopeId, surface, Guid.CreateVersion7(), existing)
                : Current(existing, placement.Id, placement.Revision, surface);
            changes.Add(new Change(record, record.Revision == 0 ? null : PlacementState.Of(record), after));
        }

        foreach (PlacementReference removed in request.Removed)
        {
            PlacementRecord record = Current(existing, removed.Id, removed.Revision, surface: null);
            changes.Add(new Change(record, PlacementState.Of(record), null));
        }

        await CommitAsync(area, scopeId, CallerId(caller), changes, rollsBack: null, cancellationToken);
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
        List<ChangeRecord> batch = await db.Changes.AsNoTracking().Where(change => change.BatchId == batchId).ToListAsync(cancellationToken);
        if (batch.Count == 0)
            throw new RpcException(new Status(StatusCode.NotFound, "That change isn't in the history."));

        (SurfaceArea area, string? scopeId) = await AuthoriseAsync(caller, batch[0].Area, batch[0].ScopeId, cancellationToken);
        List<PlacementRecord> existing = await InArea(area, scopeId).ToListAsync(cancellationToken);
        List<Change> changes = [];

        foreach (ChangeRecord change in batch)
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
            PlacementState after = Validated(surface, target);
            record ??= Create(area, scopeId, surface, change.PlacementId, existing);
            changes.Add(new Change(record, record.Revision == 0 ? null : PlacementState.Of(record), after));
        }

        await CommitAsync(area, scopeId, CallerId(caller), changes, rollsBack: batchId, cancellationToken);
        return await PlacementsAsync(area, scopeId, cancellationToken);
    }

    private async Task<(SurfaceArea Area, string? ScopeId)> AuthoriseAsync(
        ClaimsPrincipal caller, string areaKey, string? requestedScope, CancellationToken cancellationToken)
    {
        if (!surfaces.Areas.TryGetValue(areaKey, out SurfaceArea? area))
            throw new RpcException(new Status(StatusCode.NotFound, $"There's no area '{areaKey}' to arrange."));

        string? scopeId = string.IsNullOrWhiteSpace(requestedScope) ? null : requestedScope.Trim();
        if (area.ScopeKind == ScopeKind.Global && scopeId is not null)
            throw Invalid($"{area.Title} is arranged once for everyone, so it doesn't take a scope.");
        if (area.ScopeKind != ScopeKind.Global && scopeId is null)
            throw Invalid($"Choose which {area.ScopeKind.Name} to arrange.");

        if (!await access.IsAllowedAsync(caller, area.EditPermission.Node, scopeId, cancellationToken))
            throw new RpcException(new Status(StatusCode.PermissionDenied, $"You can't arrange {area.Title}."));

        return (area, scopeId);
    }

    private PlacementState Validated(Surface surface, PlacementState state)
    {
        List<string> problems = [.. surface.ProblemsWith(state.Values)];
        if (!Audience.TryParse(state.Audience, out Audience? audience))
            problems.Add($"'{state.Audience}' isn't an audience.");
        else if (audience.Node is { } node && !permissions.ByNode.ContainsKey(node))
            problems.Add($"No permission '{node}' exists to choose an audience by.");
        if (state is { StartsAt: { } starts, EndsAt: { } ends } && ends <= starts)
            problems.Add($"'{surface.Title}' has to end after it starts.");

        return problems.Count == 0 ? state with { Audience = audience!.Text } : throw Invalid(string.Join(" ", problems));
    }

    private PlacementRecord Create(SurfaceArea area, string? scopeId, Surface surface, Guid id, List<PlacementRecord> existing)
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

    private static PlacementRecord Current(List<PlacementRecord> existing, string id, long revision, Surface? surface)
    {
        Guid placementId = ParseId(id);
        PlacementRecord record = existing.SingleOrDefault(placement => placement.Id == placementId) ?? throw Conflict(surface?.Title ?? "it");
        if (record.Revision != revision)
            throw Conflict(surface?.Title ?? record.Surface);
        if (surface is not null && record.Surface != surface.Key)
            throw Invalid("A placement can't change which surface it is.");
        return record;
    }

    private async Task CommitAsync(
        SurfaceArea area, string? scopeId, Guid actor, List<Change> changes, Guid? rollsBack, CancellationToken cancellationToken)
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
                Area = area.Key,
                ScopeId = scopeId,
                PlacementId = record.Id,
                Surface = record.Surface,
                Before = before?.ToJson(),
                After = after?.ToJson(),
                At = now,
                By = actor,
                RollsBack = rollsBack,
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
    }

    private static FeedChangeRecord FeedChangeOf(PlacementRecord record, bool deleted) => new()
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

    private static byte[] Encode(Placement placement)
    {
        using MemoryStream stream = new();
        ConfigurationContractsModel.Instance.Serialize(stream, placement);
        return stream.ToArray();
    }

    private async Task<AreaPlacements> PlacementsAsync(SurfaceArea area, string? scopeId, CancellationToken cancellationToken) => new()
    {
        Placements = (await InArea(area, scopeId).AsNoTracking().OrderBy(placement => placement.Order).ToListAsync(cancellationToken))
            .Select(ToMessage)
            .ToList(),
    };

    private IQueryable<PlacementRecord> InArea(SurfaceArea area, string? scopeId) =>
        db.Placements.Where(placement => placement.Area == area.Key && placement.ScopeId == scopeId);

    private Surface SurfaceIn(SurfaceArea area, string key) =>
        surfaces.ByKey.TryGetValue(key, out Surface? surface) && surface.Area.Key == area.Key
            ? surface
            : throw Invalid($"'{key}' can't be placed in {area.Title}.");

    private static Placement ToMessage(PlacementRecord record)
    {
        Placement message = PlacementState.Of(record).ToMessage(record.Id, record.Area, record.ScopeId);
        message.Revision = record.Revision;
        message.UpdatedAt = record.UpdatedAt;
        message.UpdatedBy = record.UpdatedBy.ToString();
        return message;
    }

    private static Guid CallerId(ClaimsPrincipal caller) =>
        Caller.UserId(caller) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in again."));

    private static Guid ParseId(string id) =>
        Guid.TryParse(id, out Guid parsed) ? parsed : throw Invalid("That isn't a valid id.");

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private static RpcException Conflict(string what) =>
        new(new Status(StatusCode.Aborted, $"Someone else changed {what} while you were editing. Reload to see their change."));

    private sealed record Change(PlacementRecord Record, PlacementState? Before, PlacementState? After);
}

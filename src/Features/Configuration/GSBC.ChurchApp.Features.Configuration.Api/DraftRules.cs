using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using static GSBC.ChurchApp.Features.Configuration.Api.PlacementEditor;

namespace GSBC.ChurchApp.Features.Configuration.Api;

internal sealed class DraftRules(SurfaceCatalog surfaces)
{
    public void RefuseChangedForEveryone(IEnumerable<StoredPlacement> live, IReadOnlyList<PlacementPatch> patches)
    {
        if (DraftComposition.ChangedSince(live, patches) is { Count: > 0 } changed)
            throw new RpcException(new Status(StatusCode.FailedPrecondition,
                $"Some of this draft was changed for everyone since it was edited: {string.Join(", ", changed.Select(entry => TitleOf(entry.Patch)))}. Keep yours or use everyone's first."));
    }

    public static PlacementPatch? Settled(PlacementPatch patch, StoredPlacement live, bool keepMine)
    {
        ChangedForEveryone? changed = DraftComposition.ChangedSince([live], [patch]).SingleOrDefault();
        PlacementPatch settled = keepMine || changed is null ? patch with { Base = live } : Without(patch, changed.Fields) with { Base = live };
        bool nothingLeft = settled.Kind == PatchKind.Remove ? !keepMine : settled.Kind == PatchKind.Change && !settled.Fields.Any();
        return nothingLeft ? null : settled;
    }

    public bool HasUniqueNames(string areaKey) =>
        surfaces.ByKey.Values.Any(surface => surface.Area.Key == areaKey && surface.UniqueBy is not null);

    public void RefuseClashingNames(SurfaceArea area, IReadOnlyList<StoredPlacement> composed, IReadOnlyCollection<string> changed)
    {
        foreach (Surface surface in surfaces.ByKey.Values.Where(surface => surface.Area.Key == area.Key && surface.UniqueBy is not null))
        {
            if (UniqueNames.Clashes(surface, composed, changed) is [{ } name, ..])
                throw Invalid($"There's already a {surface.Title.ToLowerInvariant()} called “{name}”. Choose another name, or replace that one.");
        }
    }

    public static void RefuseDeletingTheChosenTheme(IReadOnlyList<StoredPlacement> before, IReadOnlyList<StoredPlacement> after)
    {
        string chosen = after.FirstOrDefault(placement => placement.Surface == AppearanceSurfaces.Theme.Surface.Key) is { } theme
            ? new SettingValues(theme.Values).Get(AppearanceSurfaces.Theme.Palette)
            : AppearanceSurfaces.Theme.Palette.Default;
        if (!ThemeChoiceSetting.IsCustom(chosen, out string themeId) || after.Any(placement => placement.Id == themeId))
            return;
        if (before.FirstOrDefault(placement => placement.Id == themeId && placement.Surface == AppearanceSurfaces.CustomTheme.Surface.Key) is { } deleted)
            throw new RpcException(new Status(StatusCode.FailedPrecondition,
                $"“{new SettingValues(deleted.Values).Get(AppearanceSurfaces.CustomTheme.Name)}” is the theme everyone sees. Choose another theme for everyone before deleting it."));
    }

    public string TitleOf(PlacementPatch patch)
    {
        Surface? surface = surfaces.ByKey.GetValueOrDefault(patch.Surface);
        string? named = surface?.Settings.OfType<TextSetting>().FirstOrDefault() is { } title && surface.Repeatable
            ? patch.ValueChanges.GetValueOrDefault(title.Key) ?? patch.Base?.Values.GetValueOrDefault(title.Key)
            : null;
        return named is null ? surface?.Title ?? patch.Surface : $"\"{named}\"";
    }

    public Notification? WaitingToPublish(DraftRecord draft, IEnumerable<DraftChangeRecord> changes, Guid? askedById, string askedBy)
    {
        DraftChangeRecord first = changes.OrderBy(change => change.Area, StringComparer.Ordinal).First();
        return surfaces.Areas.TryGetValue(first.Area, out SurfaceArea? area)
            ? new Notification(
                DraftNotifications.WaitingToPublish,
                NotificationAudience.Holding(area.PublishPermission, first.ScopeId),
                $"“{draft.Name}” is waiting to publish",
                $"{askedBy} asked for it to be published. Publish it from the admin panel.",
                Link: PreviewLink(draft),
                Subject: Subject(draft),
                Action: "Preview it") { Actor = askedById }
            : null;
    }

    public static Notification SharedWith(DraftRecord draft, Guid person, Guid? sharedById, string sharedBy) => new(
        DraftNotifications.SharedWithYou,
        NotificationAudience.Person(person),
        $"{sharedBy} shared “{draft.Name}” with you",
        "See it on your phone before anyone else does.",
        Link: PreviewLink(draft),
        Subject: Subject(draft),
        Action: "Preview it") { Actor = sharedById };

    public static string NotPublished(string reason) =>
        reason.Length <= ConfigurationDbContext.MaximumNotPublished ? reason : reason[..(ConfigurationDbContext.MaximumNotPublished - 1)] + "…";

    public static Notification DidNotPublish(DraftRecord draft, Guid scheduledBy) => new(
        DraftNotifications.NotPublished,
        NotificationAudience.Person(scheduledBy),
        $"“{draft.Name}” didn't publish",
        $"{draft.NotPublished} Open it in the admin panel's Drafts to settle it, then publish it or schedule it again.",
        Link: PreviewLink(draft),
        Subject: Subject(draft),
        Action: "Preview it") { Actor = null };

    public static string Subject(DraftRecord draft) => $"draft/{draft.Id}";

    private static string PreviewLink(DraftRecord draft) => $"/previews?draft={draft.Id}";

    private static PlacementPatch Without(PlacementPatch patch, IReadOnlyList<string> fields) => patch with
    {
        Order = fields.Contains(PatchField.Order) ? null : patch.Order,
        Hidden = fields.Contains(PatchField.Hidden) ? null : patch.Hidden,
        Audience = fields.Contains(PatchField.Audience) ? null : patch.Audience,
        SetsDates = patch.SetsDates && !fields.Contains(PatchField.Dates),
        Values = patch.ValueChanges.Where(pair => !fields.Contains(PatchField.Value(pair.Key))).ToDictionary(),
    };
}

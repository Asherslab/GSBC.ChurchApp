using GSBC.ChurchApp.Features.Configuration.Client;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Admin;

internal sealed class DraftsOfficeWeek(DraftsClient drafts, DatedChanges dated, IAccess access) : IOfficeWeekSource
{
    public IReadOnlyCollection<string> Feeds { get; } = [ConfigurationFeeds.Drafts, ConfigurationFeeds.Placements];

    public async Task<OfficeWeek> LoadAsync(DateTime from, DateTime until)
    {
        if (!access.Can(HubSurfaces.Edit))
            return OfficeWeek.Nothing;

        DraftList list = await drafts.ListAsync();
        IReadOnlyList<DatedLane> lanes = await dated.LanesAsync(list, from, until);
        return new OfficeWeek(list.Drafts.SelectMany(task => TasksFor(task, list.SeesEveryDraft)).ToList(),
            lanes.SelectMany(lane => lane.Spans).Select(ToOffice).ToList());
    }

    private static IEnumerable<OfficeTask> TasksFor(Draft draft, bool seesEveryDraft)
    {
        string href = $"app-styling/{draft.Id}";
        if (draft.Mine && draft.ToCheck > 0)
            yield return new OfficeTask(draft.Name, "Changed for everyone since you edited it", href, "Check", draft.UpdatedAt);
        else if (draft.Status == DraftStatus.WaitingToPublish)
            yield return new OfficeTask(draft.Name, draft.Mine ? "You asked to publish it" : "Waiting to publish", href, "Open", draft.UpdatedAt);
        else if (!draft.Mine && !seesEveryDraft)
            yield return new OfficeTask(draft.Name, "Shared with you to look over", $"{href}/preview", "Preview", draft.UpdatedAt);
    }

    private static OfficeSpan ToOffice(DatedSpan span) => new(span.Title, span.StartsAt, span.EndsAt, span.DraftId is null ? "app-styling" : $"app-styling/{span.DraftId}", span.Kind switch
    {
        DatedKind.Layer => OfficeSpanKind.Layer,
        DatedKind.Draft => OfficeSpanKind.Draft,
        _ => OfficeSpanKind.Everyone,
    });
}

using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.Content;
using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Features.Ministries.Client;

internal sealed class MinistryCardChoices(IMinistryReads reads) : IAppCardSource
{
    private const int MostChoices = 40;

    public IReadOnlyList<AppCardKind> Kinds => [AppCardKind.Ministry, AppCardKind.LifeGroup];

    public IReadOnlyList<string> Feeds => reads.Feeds;

    public async Task<IReadOnlyList<AppCardChoice>> ChoicesAsync(AppCardKind kind, string text)
    {
        IEnumerable<MinistrySummary> found = kind == AppCardKind.LifeGroup ? (await reads.LifeGroupsAsync()).Groups : (await reads.DirectoryAsync()).Ministries;
        string wanted = text.Trim();
        return found
            .Where(ministry => wanted.Length == 0 || ministry.Name.Contains(wanted, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(ministry => ministry.Name, StringComparer.CurrentCulture)
            .Take(MostChoices)
            .Select(ministry => new AppCardChoice(new AppCard(kind, ministry.Id), ministry.Name, ministry.Meets.Length > 0 ? ministry.Meets : ministry.Summary))
            .ToList();
    }
}

using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class ProgressTests : SermonsHost
{
    [Fact]
    public async Task ProgressIsMergedByTheRuleAndReachesOnlyThatPersonsPhones()
    {
        await ReadTheWebsiteAsync();
        string sermon = (await CatalogueAsync()).First().Id;
        Guid person = Guid.CreateVersion7(), someoneElse = Guid.CreateVersion7();
        DateTime now = Clock.UtcNow;

        await SaveAsync(person, ListeningProgress.Heard(sermon, 600, 2400, now.AddMinutes(-10)));
        await SaveAsync(person, ListeningProgress.Heard(sermon, 100, 2400, now.AddMinutes(-60)));
        Assert.Equal(600, (await StoredAsync(person, sermon)).Position);

        await SaveAsync(person, ListeningProgress.Heard(sermon, 2390, 2400, now.AddMinutes(-5)));
        await SaveAsync(person, ListeningProgress.Heard(sermon, 45, 2400, now.AddMinutes(-1)));
        ListeningEntry relistening = await StoredAsync(person, sermon);
        Assert.Equal(45, relistening.Position);
        Assert.True(ListeningProgress.IsFinished(relistening));

        await SaveAsync(person, ListeningProgress.Heard(sermon, 900, 2400, now.AddDays(3)));
        Assert.Equal(now + SermonsService.ClockSlack, (await StoredAsync(person, sermon)).HeardAt);

        List<FeedChangeRecord> rows = await LiveRowsAsync(SermonsFeeds.Progress);
        Assert.All(rows, row => Assert.Equal(person.ToString(), row.ScopeId));
        Assert.DoesNotContain(rows, row => row.ScopeId == someoneElse.ToString());
    }

    [Fact]
    public async Task ProgressForASermonThatDoesntExistIsIgnored()
    {
        Guid person = Guid.CreateVersion7();
        await SaveAsync(person, ListeningProgress.Heard(Guid.CreateVersion7().ToString(), 60, 600, Clock.UtcNow));
        Assert.Empty(await LiveRowsAsync(SermonsFeeds.Progress));
    }

    private Task SaveAsync(Guid person, ListeningEntry entry) =>
        WithAsync(provider => provider.GetRequiredService<SermonsService>().SaveAsync(person, [entry], Cancel));

    private async Task<ListeningEntry> StoredAsync(Guid person, string sermon) =>
        (await LiveRowsAsync(SermonsFeeds.Progress))
            .Where(row => row.ScopeId == person.ToString())
            .Select(row => SermonsPayloads.Decode<ListeningEntry>(row.Payload))
            .Single(entry => entry.SermonId == sermon);
}

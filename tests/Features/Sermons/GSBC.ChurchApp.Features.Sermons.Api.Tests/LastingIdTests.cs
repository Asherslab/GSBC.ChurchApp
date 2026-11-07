using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

[Integration("Moving records to their lasting ids rewrites their keys in one transaction and leaves former ids that later saves and reads resolve")]
public sealed class LastingIdTests : SermonsHost
{
    [Fact]
    public async Task RecordsReadBeforeLastingIdsMoveToThemCarryingProgressFollowsAndPicturesAndAReReadNeverDuplicates()
    {
        await ReadTheWebsiteAsync();
        Ids lasting = await IdsAsync();
        SermonRecord hope = await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Sermons.AsNoTracking()
            .SingleAsync(sermon => sermon.SourceTitle.StartsWith("Safe Harbour: Part 4"), Cancel));
        (Guid sermonWas, Guid seriesWas, Guid speakerWas) = (Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());
        await GiveRandomIdsAsync(hope.Id, sermonWas, hope.SourceSeriesId!.Value, seriesWas, hope.SourceSpeakerId!.Value, speakerWas);

        Guid person = Guid.CreateVersion7(), phone = Guid.CreateVersion7();
        await WithAsync(provider => provider.GetRequiredService<SermonsService>()
            .SaveAsync(person, [ListeningProgress.Heard(sermonWas.ToString(), 600, 2400, Clock.UtcNow.AddMinutes(-5))], Cancel));
        await WithAsync(provider => provider.GetRequiredService<SermonFollowing>()
            .FollowAsync(phone, person, new FollowRequest { SeriesId = seriesWas.ToString(), Follow = true }, Cancel));
        Media.Uses[Artwork.SeriesOwner(seriesWas)] = [Guid.CreateVersion7().ToString()];

        await WithAsync(provider => provider.GetRequiredService<SourceIdMove>().RunAsync(Cancel));

        Assert.Equal(lasting, await IdsAsync());
        Assert.Equal(hope.Id, (await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Progress.AsNoTracking().SingleAsync(Cancel))).SermonId);
        Assert.Contains((person, SermonsPermissions.SeriesFollowerRole, hope.SourceSeriesId.ToString(), false), Roles.Held);
        Assert.DoesNotContain(Roles.Held, held => held.ObjectId == seriesWas.ToString());
        Assert.Empty(Media.Uses[Artwork.SeriesOwner(seriesWas)]);
        Assert.NotEmpty(Media.Uses[Artwork.SeriesOwner(hope.SourceSeriesId.Value)]);

        List<Sermon> catalogue = await CatalogueAsync();
        Sermon moved = Assert.Single(catalogue, sermon => sermon.Id == hope.Id.ToString());
        Assert.Equal([sermonWas.ToString()], moved.FormerIds);
        Assert.Equal(hope.SourceSeriesId.ToString(), moved.SeriesId);
        Assert.DoesNotContain(catalogue, sermon => sermon.Id == sermonWas.ToString());
        Assert.DoesNotContain(await LiveRowsAsync(SermonsFeeds.Progress), row => row.RecordId.EndsWith(sermonWas.ToString("N"), StringComparison.Ordinal));

        await WithAsync(provider => provider.GetRequiredService<SermonsService>()
            .SaveAsync(person, [ListeningProgress.Heard(sermonWas.ToString(), 900, 2400, Clock.UtcNow)], Cancel));
        Assert.Equal(900, (await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Progress.AsNoTracking().SingleAsync(Cancel))).Position);
        FoundPlace? linked = await WithAsync(provider => new SeriesDestinationSource(provider.GetRequiredService<SermonsDbContext>()).FindAsync(seriesWas.ToString(), Cancel));
        Assert.Equal(hope.SourceSeriesId.ToString(), linked?.Id);

        await ReadTheWebsiteAsync(force: true);
        await WithAsync(provider => provider.GetRequiredService<SourceIdMove>().RunAsync(Cancel));
        Assert.Equal(lasting, await IdsAsync());
        Assert.Equal(4, (await CatalogueAsync()).Count);
    }

    private sealed record Ids(List<Guid> Sermons, List<Guid> Series, List<Guid> Speakers)
    {
        public bool Equals(Ids? other) => other is not null && Sermons.SequenceEqual(other.Sermons) && Series.SequenceEqual(other.Series) && Speakers.SequenceEqual(other.Speakers);

        public override int GetHashCode() => Sermons.Count;
    }

    private Task<Ids> IdsAsync() => WithAsync(async provider =>
    {
        SermonsDbContext db = provider.GetRequiredService<SermonsDbContext>();
        return new Ids(
            await db.Sermons.Select(sermon => sermon.Id).OrderBy(id => id).ToListAsync(Cancel),
            await db.Series.Select(series => series.Id).OrderBy(id => id).ToListAsync(Cancel),
            await db.Speakers.Select(speaker => speaker.Id).OrderBy(id => id).ToListAsync(Cancel));
    });

    private Task GiveRandomIdsAsync(Guid sermon, Guid sermonWas, Guid series, Guid seriesWas, Guid speaker, Guid speakerWas) => WithAsync(async provider =>
    {
        SermonsDbContext db = provider.GetRequiredService<SermonsDbContext>();
        await db.Database.ExecuteSqlAsync($"UPDATE sermons.sermons SET id = {sermonWas} WHERE id = {sermon}", Cancel);
        await db.Database.ExecuteSqlAsync($"UPDATE sermons.series SET id = {seriesWas} WHERE id = {series}", Cancel);
        await db.Database.ExecuteSqlAsync($"UPDATE sermons.sermons SET source_series_id = {seriesWas} WHERE source_series_id = {series}", Cancel);
        await db.Database.ExecuteSqlAsync($"UPDATE sermons.speakers SET id = {speakerWas} WHERE id = {speaker}", Cancel);
        await db.Database.ExecuteSqlAsync($"UPDATE sermons.speaker_aliases SET speaker_id = {speakerWas} WHERE speaker_id = {speaker}", Cancel);
        await db.Database.ExecuteSqlAsync($"UPDATE sermons.sermons SET source_speaker_id = {speakerWas} WHERE source_speaker_id = {speaker}", Cancel);
        await provider.GetRequiredService<SermonPublisher>().PublishAsync(Cancel);
    });
}

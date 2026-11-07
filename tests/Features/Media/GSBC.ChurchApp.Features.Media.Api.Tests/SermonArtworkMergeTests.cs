using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

[Integration("The migration that merges sermon artwork pairs rewrites their items, renditions and uses in SQL")]
public sealed class SermonArtworkMergeTests : MediaLibraryHost
{
    [Fact]
    public async Task SermonArtworkPairsBecomeOnePictureWithoutBreakingWhatSermonsHolds()
    {
        await using (AsyncServiceScope scope = Services.CreateAsyncScope())
        {
            MediaDbContext db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
            await db.GetService<IMigrator>().MigrateAsync("20261104155719_MediaContentHash", cancellationToken: Cancellation);
            Guid nobody = Guid.Empty;
            string largeKey = $"{new string('a', 64)}.jpg", smallKey = $"{new string('b', 64)}.jpg";
            string largeHash = new('a', 64), smallHash = new('b', 64);
            string twiceLargeHash = new('c', 64), twiceLargeAgainHash = new('d', 64), twiceSmallHash = new('e', 64);
            string twiceLargeKey = $"{twiceLargeHash}.jpg", twiceLargeAgainKey = $"{twiceLargeAgainHash}.jpg", twiceSmallKey = $"{twiceSmallHash}.jpg";
            await db.Database.ExecuteSqlAsync($"INSERT INTO media.folders (id, name, system_area, created_by, created_at) VALUES ({ArtworkFolder}, 'Sermon artwork', 'sermons', {nobody}, now())", Cancellation);
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO media.items (id, key, sha256, content_type, bytes, width, height, file_name, alt_text, uploaded_by, uploaded_at, folder_id) VALUES
                    ({LargeArtwork}, {largeKey}, {largeHash}, 'image/jpeg', 200000, 1280, 720, '0123456789ab-large.jpg', '', {nobody}, now(), {ArtworkFolder}),
                    ({SmallArtwork}, {smallKey}, {smallHash}, 'image/jpeg', 40000, 480, 270, '0123456789ab-small.jpg', '', {nobody}, now(), {ArtworkFolder}),
                    ({TwiceLarge}, {twiceLargeKey}, {twiceLargeHash}, 'image/jpeg', 210000, 1280, 720, 'fedcba987654-large.jpg', '', {nobody}, now(), {ArtworkFolder}),
                    ({TwiceLargeAgain}, {twiceLargeAgainKey}, {twiceLargeAgainHash}, 'image/jpeg', 215000, 1280, 720, 'fedcba987654-large.jpg', '', {nobody}, now(), {ArtworkFolder}),
                    ({TwiceSmall}, {twiceSmallKey}, {twiceSmallHash}, 'image/jpeg', 41000, 480, 270, 'fedcba987654-small.jpg', '', {nobody}, now(), {ArtworkFolder})
                """, Cancellation);
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO media.uses (media_id, owner) VALUES
                    ({LargeArtwork}, 'sermons/sermon/1/artwork'), ({SmallArtwork}, 'sermons/sermon/1/artwork'), ({SmallArtwork}, 'sermons/sermon/2/artwork')
                """, Cancellation);
            await db.Database.MigrateAsync(Cancellation);
        }

        MediaRef small = new() { Id = SmallArtwork.ToString(), Key = $"{new string('b', 64)}.jpg" };
        MediaRef large = new() { Id = LargeArtwork.ToString(), Key = $"{new string('a', 64)}.jpg" };
        IReadOnlyList<MediaRef> verified = await ReferencesCallAsync(references => references.VerifyAsync([large, small], Cancellation));
        await ReferencesCallAsync(references => Done(references.UseAsync("sermons/sermon/3/artwork", [large, small], Cancellation)));

        await using AsyncServiceScope after = Services.CreateAsyncScope();
        MediaDbContext merged = after.ServiceProvider.GetRequiredService<MediaDbContext>();
        Assert.Equal(new[] { LargeArtwork, TwiceLarge, TwiceLargeAgain, TwiceSmall }.Order(), (await merged.Items.Select(item => item.Id).ToListAsync(Cancellation)).Order());
        MediaRendition thumbnail = Assert.Single(await merged.Renditions.ToListAsync(Cancellation));
        Assert.Equal((LargeArtwork, small.Key), (thumbnail.MediaId, thumbnail.Key));
        Assert.Equal("0123456789ab.jpg", (await merged.Items.SingleAsync(item => item.Id == LargeArtwork, Cancellation)).FileName);
        Assert.All(verified, media => Assert.Equal((LargeArtwork.ToString(), large.Key), (media.Id, media.Key)));
        Assert.Equal(small.Key, verified[1].At(MediaSizeName.Thumbnail).Key);
        Assert.Equal(["sermons/sermon/1/artwork", "sermons/sermon/2/artwork", "sermons/sermon/3/artwork"],
            (await merged.Uses.Where(use => use.MediaId == LargeArtwork).Select(use => use.Owner).ToListAsync(Cancellation)).Order());
    }
}

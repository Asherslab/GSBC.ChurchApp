using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

[Integration("The migration that moves fonts fetched from Google out of the Library does it in SQL")]
public sealed class GoogleFontsMoveTests : MediaLibraryHost
{
    private static readonly Guid Lobster = Guid.CreateVersion7();
    private static readonly Guid LoraBold = Guid.CreateVersion7();
    private static readonly Guid ChurchFont = Guid.CreateVersion7();
    private static readonly Guid StaffFolder = Guid.CreateVersion7();

    [Fact]
    public async Task FontsFetchedFromGoogleMoveToMadeByTheAppAndStaffUploadsStay()
    {
        await using (AsyncServiceScope scope = Services.CreateAsyncScope())
        {
            MediaDbContext db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
            await db.GetService<IMigrator>().MigrateAsync("20261105140744_MediaSizes", cancellationToken: Cancellation);
            Guid nobody = Guid.Empty;
            await db.Database.ExecuteSqlAsync($"INSERT INTO media.folders (id, name, created_by, created_at) VALUES ({StaffFolder}, 'Brand', {nobody}, now())", Cancellation);
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO media.items (id, key, sha256, content_type, bytes, width, height, file_name, alt_text, uploaded_by, uploaded_at, folder_id) VALUES
                    ({Lobster}, {Key('a')}, {new string('a', 64)}, 'font/woff2', 20000, 0, 0, 'Lobster 400.woff2', '', {nobody}, now(), NULL),
                    ({LoraBold}, {Key('b')}, {new string('b', 64)}, 'font/woff2', 30000, 0, 0, 'Lora 400 700.woff2', '', {nobody}, now(), {StaffFolder}),
                    ({ChurchFont}, {Key('c')}, {new string('c', 64)}, 'font/woff2', 40000, 0, 0, 'Church Sans Regular.woff2', '', {nobody}, now(), {StaffFolder})
                """, Cancellation);
            await db.Database.MigrateAsync(Cancellation);
        }

        await using AsyncServiceScope after = Services.CreateAsyncScope();
        MediaDbContext moved = after.ServiceProvider.GetRequiredService<MediaDbContext>();
        MediaFolder fonts = await moved.Folders.SingleAsync(folder => folder.SystemArea == "fonts", Cancellation);
        Dictionary<Guid, Guid?> folders = await moved.Items.ToDictionaryAsync(item => item.Id, item => item.FolderId, Cancellation);
        Assert.Equal((fonts.Id, fonts.Id, StaffFolder), (folders[Lobster], folders[LoraBold], folders[ChurchFont]));
    }

    private static string Key(char fill) => $"{new string(fill, 64)}.woff2";
}

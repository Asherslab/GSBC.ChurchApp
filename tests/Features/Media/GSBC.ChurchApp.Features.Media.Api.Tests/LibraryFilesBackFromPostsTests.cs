using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

[Integration("The migration that puts Library files a post moved back in the Library does it in SQL")]
public sealed class LibraryFilesBackFromPostsTests : MediaLibraryHost
{
    private static readonly Guid ChurchNews = Guid.CreateVersion7();
    private static readonly Guid MinistryPosts = Guid.CreateVersion7();
    private static readonly Guid ChatFolder = Guid.CreateVersion7();
    private static readonly Guid Letterboxing = Guid.CreateVersion7();
    private static readonly Guid Schedule = Guid.CreateVersion7();
    private static readonly Guid UploadedIntoAPost = Guid.CreateVersion7();
    private static readonly Guid SharedInAChat = Guid.CreateVersion7();

    [Fact]
    public async Task FilesAPostMovedOutOfTheLibraryGoBackAndThePostsOwnUploadsAndChatFilesStay()
    {
        await using (AsyncServiceScope scope = Services.CreateAsyncScope())
        {
            MediaDbContext db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
            await db.GetService<IMigrator>().MigrateAsync("20261110144515_MediaAreas", cancellationToken: Cancellation);
            Guid nobody = Guid.Empty;
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO media.folders (id, name, system_area, system_scope, created_by, created_at) VALUES
                    ({ChurchNews}, 'Church news', 'news', NULL, {nobody}, now()),
                    ({MinistryPosts}, 'Ministry posts', 'ministry-posts', NULL, {nobody}, now()),
                    ({ChatFolder}, 'Chat files', 'chats', NULL, {nobody}, now())
                """, Cancellation);
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO media.items (id, key, sha256, content_type, bytes, width, height, file_name, alt_text, uploaded_by, uploaded_at, folder_id, area) VALUES
                    ({Letterboxing}, {Key('a', "jpg")}, {new string('a', 64)}, 'image/jpeg', 20000, 0, 0, 'letterboxing.jpeg', '', {nobody}, now(), {ChurchNews}, NULL),
                    ({Schedule}, {Key('b', "pdf")}, {new string('b', 64)}, 'application/pdf', 30000, 0, 0, '2026 MayJun schedule.pdf', '', {nobody}, now(), {MinistryPosts}, NULL),
                    ({UploadedIntoAPost}, {Key('c', "jpg")}, {new string('c', 64)}, 'image/jpeg', 40000, 0, 0, 'camp.jpg', '', {nobody}, now(), {ChurchNews}, 'news'),
                    ({SharedInAChat}, {Key('d', "jpg")}, {new string('d', 64)}, 'image/jpeg', 50000, 0, 0, 'youth.jpg', '', {nobody}, now(), {ChatFolder}, NULL)
                """, Cancellation);
            await db.Database.MigrateAsync(Cancellation);
        }

        await using AsyncServiceScope after = Services.CreateAsyncScope();
        MediaDbContext moved = after.ServiceProvider.GetRequiredService<MediaDbContext>();
        Dictionary<Guid, Guid?> folders = await moved.Items.ToDictionaryAsync(item => item.Id, item => item.FolderId, Cancellation);
        Assert.Equal((null, null, ChurchNews, ChatFolder), (folders[Letterboxing], folders[Schedule], folders[UploadedIntoAPost], folders[SharedInAChat]));
    }

    private static string Key(char fill, string extension) => $"{new string(fill, 64)}.{extension}";
}

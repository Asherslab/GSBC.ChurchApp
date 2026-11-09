using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Media.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class GoogleFontsMadeByTheApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TEMP TABLE google_fonts ON COMMIT DROP AS
                SELECT item.id FROM media.items item
                LEFT JOIN media.folders folder ON folder.id = item.folder_id
                WHERE item.content_type = 'font/woff2'
                  AND item.file_name ~ '^.+ (400|700|400 700)\.woff2$'
                  AND item.audience_permission IS NULL
                  AND folder.system_area IS NULL;

                INSERT INTO media.folders (id, parent_id, name, system_area, system_scope, created_by, created_at)
                SELECT gen_random_uuid(), NULL, 'Fonts from Google', 'fonts', NULL, '00000000-0000-0000-0000-000000000000', now()
                WHERE EXISTS (SELECT 1 FROM google_fonts)
                  AND NOT EXISTS (SELECT 1 FROM media.folders WHERE system_area = 'fonts' AND system_scope IS NULL);

                UPDATE media.items SET folder_id = (SELECT id FROM media.folders WHERE system_area = 'fonts' AND system_scope IS NULL)
                WHERE id IN (SELECT id FROM google_fonts);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

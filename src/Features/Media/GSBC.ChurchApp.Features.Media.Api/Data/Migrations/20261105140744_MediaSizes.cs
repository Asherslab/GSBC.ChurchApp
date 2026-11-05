using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Media.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MediaSizes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "original_removed_at",
                schema: "media",
                table: "items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sizing",
                schema: "media",
                table: "items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "aliases",
                schema: "media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aliases", x => x.id);
                    table.ForeignKey(
                        name: "fk_aliases_items_media_id",
                        column: x => x.media_id,
                        principalSchema: "media",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "renditions",
                schema: "media",
                columns: table => new
                {
                    media_id = table.Column<Guid>(type: "uuid", nullable: false),
                    size = table.Column<int>(type: "integer", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    content_type = table.Column<string>(type: "text", nullable: false),
                    bytes = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_renditions", x => new { x.media_id, x.size });
                    table.ForeignKey(
                        name: "fk_renditions_items_media_id",
                        column: x => x.media_id,
                        principalSchema: "media",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("UPDATE media.items SET sizing = 1 WHERE content_type LIKE 'image/%';");

            migrationBuilder.Sql("""
                CREATE TEMP TABLE sermon_artwork_pairs AS
                SELECT (array_agg(item.id) FILTER (WHERE right(item.file_name, 10) = '-large.jpg'))[1] AS large_id,
                       (array_agg(item.id) FILTER (WHERE right(item.file_name, 10) = '-small.jpg'))[1] AS small_id
                FROM media.items item
                JOIN media.folders folder ON folder.id = item.folder_id AND folder.system_area = 'sermons'
                WHERE item.file_name ~ '^[0-9a-f]{12}-(large|small)\.jpg$'
                GROUP BY left(item.file_name, 12)
                HAVING count(*) FILTER (WHERE right(item.file_name, 10) = '-large.jpg') = 1
                   AND count(*) FILTER (WHERE right(item.file_name, 10) = '-small.jpg') = 1;

                INSERT INTO media.renditions (media_id, size, key, sha256, content_type, bytes, width, height)
                SELECT pair.large_id, 2, small.key, small.sha256, small.content_type, small.bytes, small.width, small.height
                FROM sermon_artwork_pairs pair JOIN media.items small ON small.id = pair.small_id;

                INSERT INTO media.aliases (id, media_id) SELECT small_id, large_id FROM sermon_artwork_pairs;

                INSERT INTO media.uses (media_id, owner)
                SELECT pair.large_id, used.owner FROM media.uses used JOIN sermon_artwork_pairs pair ON pair.small_id = used.media_id
                ON CONFLICT DO NOTHING;

                DELETE FROM media.uses used USING sermon_artwork_pairs pair WHERE used.media_id = pair.small_id;
                DELETE FROM media.items item USING sermon_artwork_pairs pair WHERE item.id = pair.small_id;
                UPDATE media.items item SET sizing = 2 FROM sermon_artwork_pairs pair WHERE item.id = pair.large_id;

                DROP TABLE sermon_artwork_pairs;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_items_sizing",
                schema: "media",
                table: "items",
                column: "sizing",
                filter: "sizing = 1");

            migrationBuilder.CreateIndex(
                name: "ix_aliases_media_id",
                schema: "media",
                table: "aliases",
                column: "media_id");

            migrationBuilder.CreateIndex(
                name: "ix_renditions_key",
                schema: "media",
                table: "renditions",
                column: "key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aliases",
                schema: "media");

            migrationBuilder.DropTable(
                name: "renditions",
                schema: "media");

            migrationBuilder.DropIndex(
                name: "ix_items_sizing",
                schema: "media",
                table: "items");

            migrationBuilder.DropColumn(
                name: "original_removed_at",
                schema: "media",
                table: "items");

            migrationBuilder.DropColumn(
                name: "sizing",
                schema: "media",
                table: "items");
        }
    }
}

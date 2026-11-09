using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Sermons.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SermonsAppOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TEMP TABLE website_sermons ON COMMIT DROP AS
                    SELECT id, published_hash FROM sermons.sermons WHERE origin = 'website';
                CREATE TEMP TABLE kept_sermons ON COMMIT DROP AS
                    SELECT id,
                        CASE WHEN speaker_corrected THEN speaker_id ELSE source_speaker_id END AS speaker_id,
                        CASE WHEN series_corrected THEN series_id ELSE source_series_id END AS series_id
                    FROM sermons.sermons WHERE origin <> 'website';
                CREATE TEMP TABLE website_series ON COMMIT DROP AS
                    SELECT id, published_hash FROM sermons.series series
                    WHERE (series.slug IS NOT NULL OR EXISTS (SELECT 1 FROM sermons.sermons sermon JOIN website_sermons website ON website.id = sermon.id
                            WHERE series.id IN (sermon.source_series_id, sermon.series_id)))
                        AND NOT EXISTS (SELECT 1 FROM kept_sermons kept WHERE kept.series_id = series.id);
                CREATE TEMP TABLE website_speakers ON COMMIT DROP AS
                    SELECT id, published_hash FROM sermons.speakers speaker
                    WHERE EXISTS (SELECT 1 FROM sermons.sermons sermon JOIN website_sermons website ON website.id = sermon.id
                            WHERE speaker.id IN (sermon.source_speaker_id, sermon.speaker_id))
                        AND NOT EXISTS (SELECT 1 FROM kept_sermons kept WHERE kept.speaker_id = speaker.id);

                INSERT INTO sync.feed_changes (feed, record_id, version, deleted, payload)
                SELECT feed, id::text, (extract(epoch FROM now()) * 10000000)::bigint + 621355968000000000, true, '\x'::bytea FROM (
                    SELECT 'sermons.catalogue' AS feed, id, published_hash FROM website_sermons
                    UNION ALL SELECT 'sermons.series', id, published_hash FROM website_series
                    UNION ALL SELECT 'sermons.speakers', id, published_hash FROM website_speakers) gone
                WHERE published_hash IS NOT NULL;

                DO $$
                BEGIN
                    IF to_regclass('media.uses') IS NOT NULL THEN
                        INSERT INTO sync.feed_changes (feed, record_id, version, deleted, payload)
                        SELECT DISTINCT 'media.library', media_id::text, (extract(epoch FROM now()) * 10000000)::bigint + 621355968000000000, false, '\x'::bytea
                        FROM media.uses
                        WHERE split_part(owner, '/', 1) = 'sermons'
                            AND split_part(owner, '/', 3) IN (SELECT id::text FROM website_sermons UNION ALL SELECT id::text FROM website_series UNION ALL SELECT id::text FROM website_speakers);
                        DELETE FROM media.uses
                        WHERE split_part(owner, '/', 1) = 'sermons'
                            AND split_part(owner, '/', 3) IN (SELECT id::text FROM website_sermons UNION ALL SELECT id::text FROM website_series UNION ALL SELECT id::text FROM website_speakers);
                    END IF;
                END $$;

                DELETE FROM sermons.sermons WHERE id IN (SELECT id FROM website_sermons);
                DELETE FROM sermons.series WHERE id IN (SELECT id FROM website_series);
                DELETE FROM sermons.speakers WHERE id IN (SELECT id FROM website_speakers);
                """);

            migrationBuilder.Sql("""
                UPDATE sermons.sermons SET
                    title = coalesce(title, source_title),
                    speaker_id = CASE WHEN speaker_corrected THEN speaker_id ELSE source_speaker_id END,
                    series_id = CASE WHEN series_corrected THEN series_id ELSE source_series_id END,
                    service = coalesce(service, source_service),
                    you_tube_id = CASE WHEN you_tube_corrected THEN you_tube_id ELSE source_you_tube_id END;
                UPDATE sermons.sermons SET speaker_id = NULL WHERE speaker_id IS NOT NULL AND speaker_id NOT IN (SELECT id FROM sermons.speakers);
                UPDATE sermons.sermons SET series_id = NULL WHERE series_id IS NOT NULL AND series_id NOT IN (SELECT id FROM sermons.series);

                DO $$
                BEGIN
                    IF to_regclass('media.uses') IS NOT NULL THEN
                        INSERT INTO media.uses (media_id, owner)
                            SELECT use.media_id, 'sermons/series/' || series.id FROM media.uses use JOIN sermons.series series ON use.owner = 'sermons/series/' || series.id || '/artwork'
                            WHERE series.chosen_artwork IS NULL
                            ON CONFLICT DO NOTHING;
                        INSERT INTO media.uses (media_id, owner)
                            SELECT use.media_id, 'sermons/speakers/' || speaker.id FROM media.uses use JOIN sermons.speakers speaker ON use.owner = 'sermons/speakers/' || speaker.id || '/photo'
                            WHERE speaker.chosen_photo IS NULL
                            ON CONFLICT DO NOTHING;
                        DELETE FROM media.uses WHERE owner LIKE 'sermons/series/%/artwork' OR owner LIKE 'sermons/speakers/%/photo' OR owner LIKE 'sermons/sermon/%/artwork';
                    END IF;
                END $$;
                UPDATE sermons.series SET artwork = coalesce(chosen_artwork, artwork);
                UPDATE sermons.speakers SET photo = coalesce(chosen_photo, photo);
                """);

            migrationBuilder.DropTable(
                name: "artwork_addresses",
                schema: "sermons");

            migrationBuilder.DropTable(
                name: "artwork_sources",
                schema: "sermons");

            migrationBuilder.DropTable(
                name: "former_ids",
                schema: "sermons");

            migrationBuilder.DropTable(
                name: "source_state",
                schema: "sermons");

            migrationBuilder.DropIndex(
                name: "ix_sermons_external_id",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropIndex(
                name: "ix_series_slug",
                schema: "sermons",
                table: "series");

            migrationBuilder.DropColumn(
                name: "chosen_photo",
                schema: "sermons",
                table: "speakers");

            migrationBuilder.DropColumn(
                name: "photo_hash",
                schema: "sermons",
                table: "speakers");

            migrationBuilder.DropColumn(
                name: "photo_source_url",
                schema: "sermons",
                table: "speakers");

            migrationBuilder.DropColumn(
                name: "artwork_hash",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "artwork_source_url",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "audio_copied_from",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "copied_bytes",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "copy_problem",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "copy_started_at",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "external_id",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "feed_artwork_url",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "origin",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "page_read_at",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "series_corrected",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "source_series_id",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "source_service",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "source_speaker",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "source_speaker_id",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "source_you_tube_id",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "speaker_corrected",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "web_link",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "you_tube_corrected",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "artwork_hash",
                schema: "sermons",
                table: "series");

            migrationBuilder.DropColumn(
                name: "artwork_source_url",
                schema: "sermons",
                table: "series");

            migrationBuilder.DropColumn(
                name: "chosen_artwork",
                schema: "sermons",
                table: "series");

            migrationBuilder.DropColumn(
                name: "slug",
                schema: "sermons",
                table: "series");

            migrationBuilder.DropColumn(
                name: "source_name",
                schema: "sermons",
                table: "series");

            migrationBuilder.DropColumn(
                name: "source_title",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.AddColumn<string>(
                name: "passage",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "service",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "passage",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.AddColumn<string>(
                name: "source_title",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "chosen_photo",
                schema: "sermons",
                table: "speakers",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_hash",
                schema: "sermons",
                table: "speakers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_source_url",
                schema: "sermons",
                table: "speakers",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "service",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "artwork_hash",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "artwork_source_url",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "audio_copied_from",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "copied_bytes",
                schema: "sermons",
                table: "sermons",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "copy_problem",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "copy_started_at",
                schema: "sermons",
                table: "sermons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_id",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "feed_artwork_url",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "page_read_at",
                schema: "sermons",
                table: "sermons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "series_corrected",
                schema: "sermons",
                table: "sermons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "source_series_id",
                schema: "sermons",
                table: "sermons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_service",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "source_speaker",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "source_speaker_id",
                schema: "sermons",
                table: "sermons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_you_tube_id",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "speaker_corrected",
                schema: "sermons",
                table: "sermons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "web_link",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "you_tube_corrected",
                schema: "sermons",
                table: "sermons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "artwork_hash",
                schema: "sermons",
                table: "series",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "artwork_source_url",
                schema: "sermons",
                table: "series",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "chosen_artwork",
                schema: "sermons",
                table: "series",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "slug",
                schema: "sermons",
                table: "series",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_name",
                schema: "sermons",
                table: "series",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "artwork_addresses",
                schema: "sermons",
                columns: table => new
                {
                    url = table.Column<string>(type: "text", nullable: false),
                    e_tag = table.Column<string>(type: "text", nullable: true),
                    fetched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    last_modified = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artwork_addresses", x => x.url);
                });

            migrationBuilder.CreateTable(
                name: "artwork_sources",
                schema: "sermons",
                columns: table => new
                {
                    hash = table.Column<string>(type: "text", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    picture = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artwork_sources", x => x.hash);
                });

            migrationBuilder.CreateTable(
                name: "former_ids",
                schema: "sermons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    moved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    settled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_former_ids", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "source_state",
                schema: "sermons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    e_tag = table.Column<string>(type: "text", nullable: true),
                    has_read_once = table.Column<bool>(type: "boolean", nullable: false),
                    last_modified = table.Column<string>(type: "text", nullable: true),
                    problem = table.Column<string>(type: "text", nullable: true),
                    series_checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    series_problem = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_source_state", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sermons_external_id",
                schema: "sermons",
                table: "sermons",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_series_slug",
                schema: "sermons",
                table: "series",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_artwork_addresses_hash",
                schema: "sermons",
                table: "artwork_addresses",
                column: "hash");

            migrationBuilder.CreateIndex(
                name: "ix_former_ids_current_id",
                schema: "sermons",
                table: "former_ids",
                column: "current_id");
        }
    }
}

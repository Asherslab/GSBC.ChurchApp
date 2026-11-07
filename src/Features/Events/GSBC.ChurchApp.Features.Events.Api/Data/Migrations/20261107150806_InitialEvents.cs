using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Events.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "events");

            migrationBuilder.CreateTable(
                name: "breaks",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    regular_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_breaks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "calendars",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    audience = table.Column<string>(type: "text", nullable: false),
                    shown_on_new_phones = table.Column<bool>(type: "boolean", nullable: false),
                    needs_publisher = table.Column<bool>(type: "boolean", nullable: false),
                    ministry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    i_cal_source = table.Column<string>(type: "text", nullable: true),
                    i_cal_read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    i_cal_problem = table.Column<string>(type: "text", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendars", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "events",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    calendar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fields = table.Column<string>(type: "jsonb", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    start_minute = table.Column<int>(type: "integer", nullable: true),
                    end_minute = table.Column<int>(type: "integer", nullable: true),
                    showing = table.Column<int>(type: "integer", nullable: false),
                    show_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    asked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    asked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    not_on = table.Column<bool>(type: "boolean", nullable: false),
                    not_on_note = table.Column<string>(type: "text", nullable: false),
                    change_note = table.Column<string>(type: "text", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source_uid = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "published_rows",
                schema: "events",
                columns: table => new
                {
                    feed = table.Column<string>(type: "text", nullable: false),
                    record_id = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    audience = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_published_rows", x => new { x.feed, x.record_id });
                });

            migrationBuilder.CreateTable(
                name: "regular_event_breaks",
                schema: "events",
                columns: table => new
                {
                    regular_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    break_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_regular_event_breaks", x => new { x.regular_event_id, x.break_id });
                });

            migrationBuilder.CreateTable(
                name: "regular_events",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    calendar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fields = table.Column<string>(type: "jsonb", nullable: false),
                    pattern = table.Column<string>(type: "jsonb", nullable: false),
                    start_minute = table.Column<int>(type: "integer", nullable: true),
                    end_minute = table.Column<int>(type: "integer", nullable: true),
                    showing = table.Column<int>(type: "integer", nullable: false),
                    show_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    asked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    asked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    needs = table.Column<List<string>>(type: "text[]", nullable: false),
                    remind_days_before = table.Column<List<int>>(type: "integer[]", nullable: false),
                    change_note = table.Column<string>(type: "text", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_regular_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "showings",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    calendar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    regular_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    asked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    asked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    agreed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    agreed_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_showings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "specials",
                schema: "events",
                columns: table => new
                {
                    regular_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    changed = table.Column<List<string>>(type: "text[]", nullable: false),
                    fields = table.Column<string>(type: "jsonb", nullable: false),
                    start_minute = table.Column<int>(type: "integer", nullable: true),
                    end_minute = table.Column<int>(type: "integer", nullable: true),
                    not_on = table.Column<bool>(type: "boolean", nullable: false),
                    not_on_note = table.Column<string>(type: "text", nullable: false),
                    change_note = table.Column<string>(type: "text", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_specials", x => new { x.regular_event_id, x.date });
                });

            migrationBuilder.CreateIndex(
                name: "ix_breaks_regular_event_id",
                schema: "events",
                table: "breaks",
                column: "regular_event_id");

            migrationBuilder.CreateIndex(
                name: "ix_calendars_slug",
                schema: "events",
                table: "calendars",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_events_calendar_id_source_uid",
                schema: "events",
                table: "events",
                columns: new[] { "calendar_id", "source_uid" },
                unique: true,
                filter: "source_uid IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_events_calendar_id_start_date",
                schema: "events",
                table: "events",
                columns: new[] { "calendar_id", "start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_regular_event_breaks_break_id",
                schema: "events",
                table: "regular_event_breaks",
                column: "break_id");

            migrationBuilder.CreateIndex(
                name: "ix_regular_events_calendar_id",
                schema: "events",
                table: "regular_events",
                column: "calendar_id");

            migrationBuilder.CreateIndex(
                name: "ix_showings_calendar_id_event_id_regular_event_id",
                schema: "events",
                table: "showings",
                columns: new[] { "calendar_id", "event_id", "regular_event_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "breaks",
                schema: "events");

            migrationBuilder.DropTable(
                name: "calendars",
                schema: "events");

            migrationBuilder.DropTable(
                name: "events",
                schema: "events");

            migrationBuilder.DropTable(
                name: "published_rows",
                schema: "events");

            migrationBuilder.DropTable(
                name: "regular_event_breaks",
                schema: "events");

            migrationBuilder.DropTable(
                name: "regular_events",
                schema: "events");

            migrationBuilder.DropTable(
                name: "showings",
                schema: "events");

            migrationBuilder.DropTable(
                name: "specials",
                schema: "events");
        }
    }
}

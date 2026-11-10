using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Rosters.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Rosters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rosters");

            migrationBuilder.CreateTable(
                name: "assignments",
                schema: "rosters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ministry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_name = table.Column<string>(type: "text", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    replied_by = table.Column<Guid>(type: "uuid", nullable: true),
                    replied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    assigned_by = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reminded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    nudged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "away",
                schema: "rosters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from = table.Column<DateOnly>(type: "date", nullable: false),
                    to = table.Column<DateOnly>(type: "date", nullable: false),
                    note = table.Column<string>(type: "text", nullable: false),
                    made_by = table.Column<Guid>(type: "uuid", nullable: false),
                    made_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_away", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dates",
                schema: "rosters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ministry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_key = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    calendar_name = table.Column<string>(type: "text", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    all_day = table.Column<bool>(type: "boolean", nullable: false),
                    not_on = table.Column<bool>(type: "boolean", nullable: false),
                    gone = table.Column<bool>(type: "boolean", nullable: false),
                    added_by = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "positions",
                schema: "rosters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ministry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    needed = table.Column<int>(type: "integer", nullable: false),
                    needs_blue_card = table.Column<bool>(type: "boolean", nullable: false),
                    needs_training = table.Column<bool>(type: "boolean", nullable: false),
                    needs_trained = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_positions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "published",
                schema: "rosters",
                columns: table => new
                {
                    feed = table.Column<string>(type: "text", nullable: false),
                    record_id = table.Column<string>(type: "text", nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ministry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_published", x => new { x.feed, x.record_id });
                });

            migrationBuilder.CreateTable(
                name: "settings",
                schema: "rosters",
                columns: table => new
                {
                    ministry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    replies = table.Column<bool>(type: "boolean", nullable: false),
                    swap_ways = table.Column<List<string>>(type: "text[]", nullable: false),
                    who_sees = table.Column<string>(type: "text", nullable: false),
                    remind_days_before = table.Column<int>(type: "integer", nullable: false),
                    nudge_after_days = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settings", x => x.ministry_id);
                });

            migrationBuilder.CreateTable(
                name: "swaps",
                schema: "rosters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ministry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    way = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    asked_by = table.Column<Guid>(type: "uuid", nullable: false),
                    asked_to = table.Column<Guid>(type: "uuid", nullable: true),
                    offered_by = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "text", nullable: false),
                    asked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    settled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_swaps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trained",
                schema: "rosters",
                columns: table => new
                {
                    position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_by = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trained", x => new { x.position_id, x.person_id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_assignments_date_id",
                schema: "rosters",
                table: "assignments",
                column: "date_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignments_ministry_id",
                schema: "rosters",
                table: "assignments",
                column: "ministry_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignments_person_id",
                schema: "rosters",
                table: "assignments",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_away_person_id",
                schema: "rosters",
                table: "away",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_dates_ministry_id_event_key",
                schema: "rosters",
                table: "dates",
                columns: new[] { "ministry_id", "event_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dates_starts_at",
                schema: "rosters",
                table: "dates",
                column: "starts_at");

            migrationBuilder.CreateIndex(
                name: "ix_positions_ministry_id",
                schema: "rosters",
                table: "positions",
                column: "ministry_id");

            migrationBuilder.CreateIndex(
                name: "ix_published_ministry_id",
                schema: "rosters",
                table: "published",
                column: "ministry_id");

            migrationBuilder.CreateIndex(
                name: "ix_swaps_assignment_id",
                schema: "rosters",
                table: "swaps",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "ix_swaps_ministry_id",
                schema: "rosters",
                table: "swaps",
                column: "ministry_id");

            migrationBuilder.CreateIndex(
                name: "ix_trained_person_id",
                schema: "rosters",
                table: "trained",
                column: "person_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignments",
                schema: "rosters");

            migrationBuilder.DropTable(
                name: "away",
                schema: "rosters");

            migrationBuilder.DropTable(
                name: "dates",
                schema: "rosters");

            migrationBuilder.DropTable(
                name: "positions",
                schema: "rosters");

            migrationBuilder.DropTable(
                name: "published",
                schema: "rosters");

            migrationBuilder.DropTable(
                name: "settings",
                schema: "rosters");

            migrationBuilder.DropTable(
                name: "swaps",
                schema: "rosters");

            migrationBuilder.DropTable(
                name: "trained",
                schema: "rosters");
        }
    }
}

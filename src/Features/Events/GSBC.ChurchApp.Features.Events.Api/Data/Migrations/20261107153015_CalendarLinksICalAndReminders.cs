using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Events.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CalendarLinksICalAndReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "i_cal_copied_to",
                schema: "events",
                table: "calendars",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "i_cal_e_tag",
                schema: "events",
                table: "calendars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "i_cal_last_modified",
                schema: "events",
                table: "calendars",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "calendar_links",
                schema: "events",
                columns: table => new
                {
                    secret = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    install_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_links", x => x.secret);
                });

            migrationBuilder.CreateTable(
                name: "details_reminders",
                schema: "events",
                columns: table => new
                {
                    occurrence_id = table.Column<string>(type: "text", nullable: false),
                    days_before = table.Column<int>(type: "integer", nullable: false),
                    regular_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    settled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_details_reminders", x => new { x.occurrence_id, x.days_before });
                });

            migrationBuilder.CreateIndex(
                name: "ix_calendar_links_install_id",
                schema: "events",
                table: "calendar_links",
                column: "install_id");

            migrationBuilder.CreateIndex(
                name: "ix_calendar_links_user_id",
                schema: "events",
                table: "calendar_links",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_details_reminders_settled_at",
                schema: "events",
                table: "details_reminders",
                column: "settled_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calendar_links",
                schema: "events");

            migrationBuilder.DropTable(
                name: "details_reminders",
                schema: "events");

            migrationBuilder.DropColumn(
                name: "i_cal_copied_to",
                schema: "events",
                table: "calendars");

            migrationBuilder.DropColumn(
                name: "i_cal_e_tag",
                schema: "events",
                table: "calendars");

            migrationBuilder.DropColumn(
                name: "i_cal_last_modified",
                schema: "events",
                table: "calendars");
        }
    }
}

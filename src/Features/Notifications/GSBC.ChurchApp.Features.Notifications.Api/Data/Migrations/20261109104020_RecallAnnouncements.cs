using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Notifications.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RecallAnnouncements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "recalled_at",
                schema: "notifications",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "read_by_when_recalled",
                schema: "notifications",
                table: "announcements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "recalled_at",
                schema: "notifications",
                table: "announcements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "recalled_by_id",
                schema: "notifications",
                table: "announcements",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "recalled_at",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "read_by_when_recalled",
                schema: "notifications",
                table: "announcements");

            migrationBuilder.DropColumn(
                name: "recalled_at",
                schema: "notifications",
                table: "announcements");

            migrationBuilder.DropColumn(
                name: "recalled_by_id",
                schema: "notifications",
                table: "announcements");
        }
    }
}

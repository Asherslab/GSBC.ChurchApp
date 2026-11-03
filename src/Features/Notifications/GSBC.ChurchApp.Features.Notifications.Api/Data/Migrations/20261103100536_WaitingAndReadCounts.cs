using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Notifications.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class WaitingAndReadCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "action",
                schema: "notifications",
                table: "notifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "in_admin_panel",
                schema: "notifications",
                table: "notifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "settled_at",
                schema: "notifications",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "waits",
                schema: "notifications",
                table: "notifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "notification_id",
                schema: "notifications",
                table: "announcements",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "action",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "in_admin_panel",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "settled_at",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "waits",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "notification_id",
                schema: "notifications",
                table: "announcements");
        }
    }
}

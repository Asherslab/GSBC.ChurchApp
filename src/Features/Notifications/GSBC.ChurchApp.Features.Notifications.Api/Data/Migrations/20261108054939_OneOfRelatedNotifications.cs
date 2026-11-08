using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Notifications.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class OneOfRelatedNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "one_of",
                schema: "notifications",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rank",
                schema: "notifications",
                table: "notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "one_of",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "rank",
                schema: "notifications",
                table: "notifications");
        }
    }
}

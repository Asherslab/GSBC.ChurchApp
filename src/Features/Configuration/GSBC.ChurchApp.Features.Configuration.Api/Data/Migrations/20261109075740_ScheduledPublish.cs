using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Configuration.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ScheduledPublish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "not_published",
                schema: "config",
                table: "drafts",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "publish_at",
                schema: "config",
                table: "drafts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "publish_scheduled_by",
                schema: "config",
                table: "drafts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_drafts_publish_at",
                schema: "config",
                table: "drafts",
                column: "publish_at",
                filter: "publish_at IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_drafts_publish_at",
                schema: "config",
                table: "drafts");

            migrationBuilder.DropColumn(
                name: "not_published",
                schema: "config",
                table: "drafts");

            migrationBuilder.DropColumn(
                name: "publish_at",
                schema: "config",
                table: "drafts");

            migrationBuilder.DropColumn(
                name: "publish_scheduled_by",
                schema: "config",
                table: "drafts");
        }
    }
}

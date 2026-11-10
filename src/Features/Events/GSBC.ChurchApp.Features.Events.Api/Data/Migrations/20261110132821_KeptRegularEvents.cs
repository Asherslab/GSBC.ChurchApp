using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Events.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class KeptRegularEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "keeper",
                schema: "events",
                table: "regular_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "kept_by",
                schema: "events",
                table: "regular_events",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "opens_at",
                schema: "events",
                table: "regular_events",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_regular_events_keeper",
                schema: "events",
                table: "regular_events",
                column: "keeper",
                unique: true,
                filter: "keeper IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_regular_events_keeper",
                schema: "events",
                table: "regular_events");

            migrationBuilder.DropColumn(
                name: "keeper",
                schema: "events",
                table: "regular_events");

            migrationBuilder.DropColumn(
                name: "kept_by",
                schema: "events",
                table: "regular_events");

            migrationBuilder.DropColumn(
                name: "opens_at",
                schema: "events",
                table: "regular_events");
        }
    }
}

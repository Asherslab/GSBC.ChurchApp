using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Feedback.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FeedbackFromTheAdminPanel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "browser",
                schema: "feedback",
                table: "tickets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "browser_system",
                schema: "feedback",
                table: "tickets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "colour_scheme",
                schema: "feedback",
                table: "tickets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "page_title",
                schema: "feedback",
                table: "tickets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "recent_pages",
                schema: "feedback",
                table: "tickets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "source",
                schema: "feedback",
                table: "tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "window",
                schema: "feedback",
                table: "tickets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_tickets_source_sent_at",
                schema: "feedback",
                table: "tickets",
                columns: new[] { "source", "sent_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tickets_source_sent_at",
                schema: "feedback",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "browser",
                schema: "feedback",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "browser_system",
                schema: "feedback",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "colour_scheme",
                schema: "feedback",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "page_title",
                schema: "feedback",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "recent_pages",
                schema: "feedback",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "source",
                schema: "feedback",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "window",
                schema: "feedback",
                table: "tickets");
        }
    }
}

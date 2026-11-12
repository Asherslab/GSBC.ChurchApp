using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Feedback.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FeedbackTrailAndScreenshotsFromThePhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "screenshots",
                schema: "feedback",
                table: "tickets",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "screenshots",
                schema: "feedback",
                table: "tickets");
        }
    }
}

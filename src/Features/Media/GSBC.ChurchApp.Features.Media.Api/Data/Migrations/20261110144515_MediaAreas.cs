using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Media.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MediaAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "area",
                schema: "media",
                table: "pending_uploads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "area",
                schema: "media",
                table: "items",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "area",
                schema: "media",
                table: "pending_uploads");

            migrationBuilder.DropColumn(
                name: "area",
                schema: "media",
                table: "items");
        }
    }
}

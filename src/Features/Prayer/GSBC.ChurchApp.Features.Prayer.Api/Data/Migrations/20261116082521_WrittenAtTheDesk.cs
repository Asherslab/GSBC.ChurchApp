using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Prayer.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class WrittenAtTheDesk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "for_someone_else",
                schema: "prayer",
                table: "requests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "from_church",
                schema: "prayer",
                table: "requests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "church",
                schema: "prayer",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_church", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "church",
                schema: "prayer");

            migrationBuilder.DropColumn(
                name: "for_someone_else",
                schema: "prayer",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "from_church",
                schema: "prayer",
                table: "requests");
        }
    }
}

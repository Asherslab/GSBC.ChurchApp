using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Ministries.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AskWaitsForParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "waiting_for_parent",
                schema: "ministries",
                table: "asks",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "waiting_for_parent",
                schema: "ministries",
                table: "asks");
        }
    }
}

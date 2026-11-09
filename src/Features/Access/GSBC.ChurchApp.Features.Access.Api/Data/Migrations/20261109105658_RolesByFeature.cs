using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Access.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RolesByFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "feature",
                schema: "access",
                table: "roles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "hidden",
                schema: "access",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "position",
                schema: "access",
                table: "roles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "everywhere",
                schema: "access",
                table: "role_patterns",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "feature",
                schema: "access",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "hidden",
                schema: "access",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "position",
                schema: "access",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "everywhere",
                schema: "access",
                table: "role_patterns");
        }
    }
}

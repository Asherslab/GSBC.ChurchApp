using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Access.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RoleAssignersAndTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "applies_to_children",
                schema: "access",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "role_assigners",
                schema: "access",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigner_role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_assigners", x => new { x.role_id, x.assigner_role_id });
                    table.ForeignKey(
                        name: "fk_role_assigners_roles_assigner_role_id",
                        column: x => x.assigner_role_id,
                        principalSchema: "access",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_assigners_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "access",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_role_assigners_assigner_role_id",
                schema: "access",
                table: "role_assigners",
                column: "assigner_role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_assigners",
                schema: "access");

            migrationBuilder.DropColumn(
                name: "applies_to_children",
                schema: "access",
                table: "roles");
        }
    }
}

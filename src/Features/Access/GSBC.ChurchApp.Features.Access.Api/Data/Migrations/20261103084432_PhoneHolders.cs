using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Access.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PhoneHolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "phone_holdable",
                schema: "access",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "phone_assignments",
                schema: "access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    install_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_object_id = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phone_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_phone_assignments_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "access",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_phone_assignments_install_id_role_id_scope_object_id",
                schema: "access",
                table: "phone_assignments",
                columns: new[] { "install_id", "role_id", "scope_object_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_phone_assignments_role_id",
                schema: "access",
                table: "phone_assignments",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "phone_assignments",
                schema: "access");

            migrationBuilder.DropColumn(
                name: "phone_holdable",
                schema: "access",
                table: "roles");
        }
    }
}

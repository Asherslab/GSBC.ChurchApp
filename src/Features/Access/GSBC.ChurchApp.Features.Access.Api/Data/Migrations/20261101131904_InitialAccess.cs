using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Access.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "access");

            migrationBuilder.CreateTable(
                name: "catalog",
                schema: "access",
                columns: table => new
                {
                    node = table.Column<string>(type: "text", nullable: false),
                    scope_kind = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    sensitive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog", x => x.node);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_key = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    scope_kind = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "assignments",
                schema: "access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_object_id = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_assignments_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "access",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_includes",
                schema: "access",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    included_role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_includes", x => new { x.role_id, x.included_role_id });
                    table.ForeignKey(
                        name: "fk_role_includes_roles_included_role_id",
                        column: x => x.included_role_id,
                        principalSchema: "access",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_includes_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "access",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_patterns",
                schema: "access",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pattern = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_patterns", x => new { x.role_id, x.pattern });
                    table.ForeignKey(
                        name: "fk_role_patterns_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "access",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assignments_role_id",
                schema: "access",
                table: "assignments",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignments_user_id",
                schema: "access",
                table: "assignments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_includes_included_role_id",
                schema: "access",
                table: "role_includes",
                column: "included_role_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_system_key",
                schema: "access",
                table: "roles",
                column: "system_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignments",
                schema: "access");

            migrationBuilder.DropTable(
                name: "catalog",
                schema: "access");

            migrationBuilder.DropTable(
                name: "role_includes",
                schema: "access");

            migrationBuilder.DropTable(
                name: "role_patterns",
                schema: "access");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "access");
        }
    }
}

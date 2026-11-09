using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.People.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class People : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "people");

            migrationBuilder.CreateTable(
                name: "asks",
                schema: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    child_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_email = table.Column<string>(type: "text", nullable: false),
                    asked_by = table.Column<Guid>(type: "uuid", nullable: false),
                    asked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "links",
                schema: "people",
                columns: table => new
                {
                    child_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    how = table.Column<string>(type: "text", nullable: false),
                    linked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    linked_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_links", x => new { x.child_id, x.parent_id });
                });

            migrationBuilder.CreateTable(
                name: "people",
                schema: "people",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    says_adult = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_people", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "safeguarding",
                schema: "people",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_number = table.Column<string>(type: "text", nullable: true),
                    card_status = table.Column<string>(type: "text", nullable: true),
                    card_expires = table.Column<DateOnly>(type: "date", nullable: true),
                    card_checked = table.Column<DateOnly>(type: "date", nullable: true),
                    training_done = table.Column<DateOnly>(type: "date", nullable: true),
                    training_due = table.Column<DateOnly>(type: "date", nullable: true),
                    source = table.Column<string>(type: "text", nullable: false),
                    external_id = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_safeguarding", x => x.user_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asks_child_id_parent_email",
                schema: "people",
                table: "asks",
                columns: new[] { "child_id", "parent_email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asks_parent_email",
                schema: "people",
                table: "asks",
                column: "parent_email");

            migrationBuilder.CreateIndex(
                name: "ix_links_parent_id",
                schema: "people",
                table: "links",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_safeguarding_source_external_id",
                schema: "people",
                table: "safeguarding",
                columns: new[] { "source", "external_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asks",
                schema: "people");

            migrationBuilder.DropTable(
                name: "links",
                schema: "people");

            migrationBuilder.DropTable(
                name: "people",
                schema: "people");

            migrationBuilder.DropTable(
                name: "safeguarding",
                schema: "people");
        }
    }
}

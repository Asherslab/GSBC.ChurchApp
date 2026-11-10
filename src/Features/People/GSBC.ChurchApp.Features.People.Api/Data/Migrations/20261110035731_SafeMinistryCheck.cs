using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.People.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SafeMinistryCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_safeguarding_source_external_id",
                schema: "people",
                table: "safeguarding");

            migrationBuilder.AddColumn<List<string>>(
                name: "check_reasons",
                schema: "people",
                table: "safeguarding",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]");

            migrationBuilder.AddColumn<DateTime>(
                name: "linked_at",
                schema: "people",
                table: "safeguarding",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "linked_by",
                schema: "people",
                table: "safeguarding",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "linked_how",
                schema: "people",
                table: "safeguarding",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "other_training_done",
                schema: "people",
                table: "safeguarding",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "other_training_name",
                schema: "people",
                table: "safeguarding",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "smc_status",
                schema: "people",
                table: "safeguarding",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "smc_status_words",
                schema: "people",
                table: "safeguarding",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "synced_at",
                schema: "people",
                table: "safeguarding",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "training_progress",
                schema: "people",
                table: "safeguarding",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "smc_people",
                schema: "people",
                columns: table => new
                {
                    smc_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    link_by_hand_only = table.Column<bool>(type: "boolean", nullable: false),
                    why = table.Column<string>(type: "text", nullable: true),
                    seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_smc_people", x => x.smc_id);
                });

            migrationBuilder.CreateTable(
                name: "smc_sync",
                schema: "people",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    last_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_succeeded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    problem = table.Column<string>(type: "text", nullable: true),
                    people = table.Column<int>(type: "integer", nullable: false),
                    linked = table.Column<int>(type: "integer", nullable: false),
                    to_match = table.Column<int>(type: "integer", nullable: false),
                    to_check = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_smc_sync", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_safeguarding_source_external_id",
                schema: "people",
                table: "safeguarding",
                columns: new[] { "source", "external_id" },
                unique: true,
                filter: "external_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "smc_people",
                schema: "people");

            migrationBuilder.DropTable(
                name: "smc_sync",
                schema: "people");

            migrationBuilder.DropIndex(
                name: "ix_safeguarding_source_external_id",
                schema: "people",
                table: "safeguarding");

            migrationBuilder.DropColumn(
                name: "check_reasons",
                schema: "people",
                table: "safeguarding");

            migrationBuilder.DropColumn(
                name: "linked_at",
                schema: "people",
                table: "safeguarding");

            migrationBuilder.DropColumn(
                name: "linked_by",
                schema: "people",
                table: "safeguarding");

            migrationBuilder.DropColumn(
                name: "linked_how",
                schema: "people",
                table: "safeguarding");

            migrationBuilder.DropColumn(
                name: "other_training_done",
                schema: "people",
                table: "safeguarding");

            migrationBuilder.DropColumn(
                name: "other_training_name",
                schema: "people",
                table: "safeguarding");

            migrationBuilder.DropColumn(
                name: "smc_status",
                schema: "people",
                table: "safeguarding");

            migrationBuilder.DropColumn(
                name: "smc_status_words",
                schema: "people",
                table: "safeguarding");

            migrationBuilder.DropColumn(
                name: "synced_at",
                schema: "people",
                table: "safeguarding");

            migrationBuilder.DropColumn(
                name: "training_progress",
                schema: "people",
                table: "safeguarding");

            migrationBuilder.CreateIndex(
                name: "ix_safeguarding_source_external_id",
                schema: "people",
                table: "safeguarding",
                columns: new[] { "source", "external_id" });
        }
    }
}

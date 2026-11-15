using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Outreach.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CampaignStages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "bundle_size",
                schema: "outreach",
                table: "campaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "bundles_made",
                schema: "outreach",
                table: "campaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "stage",
                schema: "outreach",
                table: "campaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "stage_set_on",
                schema: "outreach",
                table: "campaigns",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateTime>(
                name: "told_ending_at",
                schema: "outreach",
                table: "campaigns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "wrap_up_days",
                schema: "outreach",
                table: "campaigns",
                type: "integer",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.Sql("""
                UPDATE outreach.campaigns SET
                    stage_set_on = (now() AT TIME ZONE 'Australia/Brisbane')::date,
                    stage = CASE
                        WHEN general THEN 0
                        WHEN ends_on < (now() AT TIME ZONE 'Australia/Brisbane')::date THEN 3
                        WHEN ends_on - 7 <= (now() AT TIME ZONE 'Australia/Brisbane')::date THEN 2
                        WHEN starts_on > (now() AT TIME ZONE 'Australia/Brisbane')::date THEN 1
                        ELSE 0
                    END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bundle_size",
                schema: "outreach",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "bundles_made",
                schema: "outreach",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "stage",
                schema: "outreach",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "stage_set_on",
                schema: "outreach",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "told_ending_at",
                schema: "outreach",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "wrap_up_days",
                schema: "outreach",
                table: "campaigns");
        }
    }
}

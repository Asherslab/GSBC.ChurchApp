using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Outreach.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class OutreachDayPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "draft",
                schema: "outreach",
                table: "days",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "routes_published_at",
                schema: "outreach",
                table: "days",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "draft",
                schema: "outreach",
                table: "days");

            migrationBuilder.DropColumn(
                name: "routes_published_at",
                schema: "outreach",
                table: "days");
        }
    }
}

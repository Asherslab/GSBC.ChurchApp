using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Ministries.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllYearGroupsSitOut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sit_outs",
                schema: "ministries",
                columns: table => new
                {
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    round_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note = table.Column<string>(type: "text", nullable: false),
                    instead_id = table.Column<Guid>(type: "uuid", nullable: true),
                    marked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sit_outs", x => new { x.group_id, x.round_id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_sit_outs_round_id",
                schema: "ministries",
                table: "sit_outs",
                column: "round_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sit_outs",
                schema: "ministries");
        }
    }
}

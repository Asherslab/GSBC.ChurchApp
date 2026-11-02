using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Platform.Data.Migrations.Sync
{
    /// <inheritdoc />
    public partial class AudienceMarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audience_marks",
                schema: "sync",
                columns: table => new
                {
                    session_id = table.Column<string>(type: "text", nullable: false),
                    feed = table.Column<string>(type: "text", nullable: false),
                    fingerprint = table.Column<string>(type: "text", nullable: false),
                    seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audience_marks", x => new { x.session_id, x.feed });
                });

            migrationBuilder.CreateIndex(
                name: "ix_audience_marks_seen_at",
                schema: "sync",
                table: "audience_marks",
                column: "seen_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audience_marks",
                schema: "sync");
        }
    }
}

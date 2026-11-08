using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Events.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class WeeklyDigest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "digests",
                schema: "events",
                columns: table => new
                {
                    start = table.Column<DateOnly>(type: "date", nullable: false),
                    weeks = table.Column<int>(type: "integer", nullable: false),
                    calendar_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: true),
                    entries = table.Column<string>(type: "jsonb", nullable: false),
                    sent = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digests", x => x.start);
                });

            migrationBuilder.CreateIndex(
                name: "ix_digests_sent_at",
                schema: "events",
                table: "digests",
                column: "sent_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "digests",
                schema: "events");
        }
    }
}

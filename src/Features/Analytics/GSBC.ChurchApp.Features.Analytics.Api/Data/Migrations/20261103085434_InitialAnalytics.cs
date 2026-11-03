using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GSBC.ChurchApp.Features.Analytics.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.CreateTable(
                name: "daily_counts",
                schema: "analytics",
                columns: table => new
                {
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    measure = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    group = table.Column<int>(type: "integer", nullable: false),
                    platform = table.Column<int>(type: "integer", nullable: false),
                    app_version = table.Column<string>(type: "text", nullable: false),
                    count = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_counts", x => new { x.day, x.measure, x.subject, x.group, x.platform, x.app_version });
                });

            migrationBuilder.CreateTable(
                name: "error_kinds",
                schema: "analytics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "text", nullable: false),
                    origin = table.Column<int>(type: "integer", nullable: false),
                    native_crash = table.Column<bool>(type: "boolean", nullable: false),
                    exception_type = table.Column<string>(type: "text", nullable: false),
                    frames = table.Column<List<string>>(type: "text[]", nullable: false),
                    where = table.Column<string>(type: "text", nullable: false),
                    first_seen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    count = table.Column<long>(type: "bigint", nullable: false),
                    notified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_error_kinds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "failed_calls",
                schema: "analytics",
                columns: table => new
                {
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    origin = table.Column<int>(type: "integer", nullable: false),
                    method = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    app_version = table.Column<string>(type: "text", nullable: false),
                    count = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_failed_calls", x => new { x.day, x.origin, x.method, x.status, x.app_version });
                });

            migrationBuilder.CreateTable(
                name: "monthly_counts",
                schema: "analytics",
                columns: table => new
                {
                    month = table.Column<DateOnly>(type: "date", nullable: false),
                    measure = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    group = table.Column<int>(type: "integer", nullable: false),
                    platform = table.Column<int>(type: "integer", nullable: false),
                    app_version = table.Column<string>(type: "text", nullable: false),
                    count = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_monthly_counts", x => new { x.month, x.measure, x.subject, x.group, x.platform, x.app_version });
                });

            migrationBuilder.CreateTable(
                name: "reports_seen",
                schema: "analytics",
                columns: table => new
                {
                    request_id = table.Column<string>(type: "text", nullable: false),
                    seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports_seen", x => x.request_id);
                });

            migrationBuilder.CreateTable(
                name: "error_reports",
                schema: "analytics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    kind_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    platform = table.Column<int>(type: "integer", nullable: false),
                    app_version = table.Column<string>(type: "text", nullable: false),
                    app_build = table.Column<string>(type: "text", nullable: false),
                    os_major = table.Column<int>(type: "integer", nullable: false),
                    development_build = table.Column<bool>(type: "boolean", nullable: false),
                    route = table.Column<string>(type: "text", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_error_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_error_reports_error_kinds_kind_id",
                        column: x => x.kind_id,
                        principalSchema: "analytics",
                        principalTable: "error_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_daily_counts_measure_day",
                schema: "analytics",
                table: "daily_counts",
                columns: new[] { "measure", "day" });

            migrationBuilder.CreateIndex(
                name: "ix_error_kinds_fingerprint",
                schema: "analytics",
                table: "error_kinds",
                column: "fingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_error_kinds_last_seen",
                schema: "analytics",
                table: "error_kinds",
                column: "last_seen");

            migrationBuilder.CreateIndex(
                name: "ix_error_reports_kind_id_day",
                schema: "analytics",
                table: "error_reports",
                columns: new[] { "kind_id", "day" });

            migrationBuilder.CreateIndex(
                name: "ix_error_reports_received_at",
                schema: "analytics",
                table: "error_reports",
                column: "received_at");

            migrationBuilder.CreateIndex(
                name: "ix_reports_seen_seen_at",
                schema: "analytics",
                table: "reports_seen",
                column: "seen_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_counts",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "error_reports",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "failed_calls",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "monthly_counts",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "reports_seen",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "error_kinds",
                schema: "analytics");
        }
    }
}

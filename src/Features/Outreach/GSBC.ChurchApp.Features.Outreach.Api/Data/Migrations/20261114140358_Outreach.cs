using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GSBC.ChurchApp.Features.Outreach.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Outreach : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "outreach");

            migrationBuilder.CreateTable(
                name: "areas",
                schema: "outreach",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    area_lats = table.Column<List<double>>(type: "double precision[]", nullable: false),
                    area_lngs = table.Column<List<double>>(type: "double precision[]", nullable: false),
                    note = table.Column<string>(type: "text", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_areas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "campaigns",
                schema: "outreach",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    handed_out = table.Column<string>(type: "text", nullable: false),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: true),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: true),
                    area_lats = table.Column<List<double>>(type: "double precision[]", nullable: false),
                    area_lngs = table.Column<List<double>>(type: "double precision[]", nullable: false),
                    regions = table.Column<List<string>>(type: "text[]", nullable: false),
                    leaderboard = table.Column<bool>(type: "boolean", nullable: false),
                    general = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaigns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "coverage",
                schema: "outreach",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    region = table.Column<string>(type: "text", nullable: false),
                    region_version = table.Column<long>(type: "bigint", nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    side = table.Column<int>(type: "integer", nullable: false),
                    from = table.Column<int>(type: "integer", nullable: false),
                    to = table.Column<int>(type: "integer", nullable: false),
                    middle_lat = table.Column<double>(type: "double precision", nullable: false),
                    middle_lng = table.Column<double>(type: "double precision", nullable: false),
                    metres = table.Column<int>(type: "integer", nullable: false),
                    letterboxes = table.Column<int>(type: "integer", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    walk_id = table.Column<Guid>(type: "uuid", nullable: true),
                    day_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coverage", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "days",
                schema: "outreach",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    meeting_lat = table.Column<double>(type: "double precision", nullable: false),
                    meeting_lng = table.Column<double>(type: "double precision", nullable: false),
                    meeting_place = table.Column<string>(type: "text", nullable: false),
                    note = table.Column<string>(type: "text", nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    published = table.Column<bool>(type: "boolean", nullable: false),
                    on_phones = table.Column<bool>(type: "boolean", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_days", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "doors",
                schema: "outreach",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    walk_id = table.Column<Guid>(type: "uuid", nullable: true),
                    install_id = table.Column<Guid>(type: "uuid", nullable: true),
                    address_key = table.Column<long>(type: "bigint", nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    side = table.Column<int>(type: "integer", nullable: false),
                    along = table.Column<int>(type: "integer", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "exclusion_addresses",
                schema: "outreach",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_key = table.Column<long>(type: "bigint", nullable: false),
                    region = table.Column<string>(type: "text", nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    side = table.Column<int>(type: "integer", nullable: false),
                    along = table.Column<int>(type: "integer", nullable: false),
                    lat = table.Column<double>(type: "double precision", nullable: false),
                    lng = table.Column<double>(type: "double precision", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "text", nullable: false),
                    caller = table.Column<string>(type: "text", nullable: false),
                    show_marker = table.Column<bool>(type: "boolean", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exclusion_addresses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "exclusion_zones",
                schema: "outreach",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    area_lats = table.Column<List<double>>(type: "double precision[]", nullable: false),
                    area_lngs = table.Column<List<double>>(type: "double precision[]", nullable: false),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "text", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exclusion_zones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "follow_ups",
                schema: "outreach",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_key = table.Column<long>(type: "bigint", nullable: false),
                    region = table.Column<string>(type: "text", nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    side = table.Column<int>(type: "integer", nullable: false),
                    along = table.Column<int>(type: "integer", nullable: false),
                    lat = table.Column<double>(type: "double precision", nullable: false),
                    lng = table.Column<double>(type: "double precision", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    contact = table.Column<string>(type: "text", nullable: false),
                    note = table.Column<string>(type: "text", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    done_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    install_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_follow_ups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "outreach",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    address_key = table.Column<long>(type: "bigint", nullable: false),
                    region = table.Column<string>(type: "text", nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    side = table.Column<int>(type: "integer", nullable: false),
                    along = table.Column<int>(type: "integer", nullable: false),
                    lat = table.Column<double>(type: "double precision", nullable: false),
                    lng = table.Column<double>(type: "double precision", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    note = table.Column<string>(type: "text", nullable: false),
                    photo = table.Column<byte[]>(type: "bytea", nullable: true),
                    state = table.Column<int>(type: "integer", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    install_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                schema: "outreach",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    payload = table.Column<byte[]>(type: "bytea", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skips",
                schema: "outreach",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    address_key = table.Column<long>(type: "bigint", nullable: false),
                    region = table.Column<string>(type: "text", nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    side = table.Column<int>(type: "integer", nullable: false),
                    along = table.Column<int>(type: "integer", nullable: false),
                    lat = table.Column<double>(type: "double precision", nullable: false),
                    lng = table.Column<double>(type: "double precision", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    install_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skips", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "walkers",
                schema: "outreach",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    by_name = table.Column<bool>(type: "boolean", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_walkers", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "walks",
                schema: "outreach",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    install_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    campaigns = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    day_id = table.Column<Guid>(type: "uuid", nullable: true),
                    route_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended = table.Column<bool>(type: "boolean", nullable: false),
                    metres = table.Column<int>(type: "integer", nullable: false),
                    letterboxes = table.Column<int>(type: "integer", nullable: false),
                    doors = table.Column<int>(type: "integer", nullable: false),
                    conversations = table.Column<int>(type: "integer", nullable: false),
                    with_partner = table.Column<bool>(type: "boolean", nullable: false),
                    doubtful = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_walks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "routes",
                schema: "outreach",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<byte[]>(type: "bytea", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_routes", x => x.id);
                    table.ForeignKey(
                        name: "fk_routes_days_day_id",
                        column: x => x.day_id,
                        principalSchema: "outreach",
                        principalTable: "days",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "walk_pieces",
                schema: "outreach",
                columns: table => new
                {
                    walk_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seq = table.Column<int>(type: "integer", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    metres = table.Column<int>(type: "integer", nullable: false),
                    counted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_walk_pieces", x => new { x.walk_id, x.seq });
                    table.ForeignKey(
                        name: "fk_walk_pieces_walks_walk_id",
                        column: x => x.walk_id,
                        principalSchema: "outreach",
                        principalTable: "walks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "claims",
                schema: "outreach",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_id = table.Column<Guid>(type: "uuid", nullable: false),
                    install_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_claims_routes_route_id",
                        column: x => x.route_id,
                        principalSchema: "outreach",
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_claims_day_id",
                schema: "outreach",
                table: "claims",
                column: "day_id");

            migrationBuilder.CreateIndex(
                name: "ix_claims_install_id_at",
                schema: "outreach",
                table: "claims",
                columns: new[] { "install_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_claims_route_id",
                schema: "outreach",
                table: "claims",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "ix_claims_user_id",
                schema: "outreach",
                table: "claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_coverage_at",
                schema: "outreach",
                table: "coverage",
                column: "at");

            migrationBuilder.CreateIndex(
                name: "ix_coverage_campaign_id_street",
                schema: "outreach",
                table: "coverage",
                columns: new[] { "campaign_id", "street" });

            migrationBuilder.CreateIndex(
                name: "ix_coverage_walk_id",
                schema: "outreach",
                table: "coverage",
                column: "walk_id");

            migrationBuilder.CreateIndex(
                name: "ix_days_starts_at",
                schema: "outreach",
                table: "days",
                column: "starts_at");

            migrationBuilder.CreateIndex(
                name: "ix_doors_at",
                schema: "outreach",
                table: "doors",
                column: "at");

            migrationBuilder.CreateIndex(
                name: "ix_follow_ups_install_id_at",
                schema: "outreach",
                table: "follow_ups",
                columns: new[] { "install_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_follow_ups_user_id",
                schema: "outreach",
                table: "follow_ups",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_install_id_at",
                schema: "outreach",
                table: "reports",
                columns: new[] { "install_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_reports_user_id",
                schema: "outreach",
                table: "reports",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_routes_code",
                schema: "outreach",
                table: "routes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_routes_day_id",
                schema: "outreach",
                table: "routes",
                column: "day_id");

            migrationBuilder.CreateIndex(
                name: "ix_skips_install_id_at",
                schema: "outreach",
                table: "skips",
                columns: new[] { "install_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_skips_user_id",
                schema: "outreach",
                table: "skips",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_walks_install_id_started_at",
                schema: "outreach",
                table: "walks",
                columns: new[] { "install_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_walks_started_at",
                schema: "outreach",
                table: "walks",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ix_walks_user_id",
                schema: "outreach",
                table: "walks",
                column: "user_id");

            migrationBuilder.Sql("""
                INSERT INTO outreach.campaigns (id, name, handed_out, area_lats, area_lngs, regions, leaderboard, general, created_at, changed_at)
                VALUES ('6f0e7a52-3b1d-4c6e-9a7f-0e1c2d3b4a50', 'General outreach', '', '{}', '{}', '{}', false, true, now(), now())
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "areas",
                schema: "outreach");

            migrationBuilder.DropTable(
                name: "campaigns",
                schema: "outreach");

            migrationBuilder.DropTable(
                name: "claims",
                schema: "outreach");

            migrationBuilder.DropTable(
                name: "coverage",
                schema: "outreach");

            migrationBuilder.DropTable(
                name: "doors",
                schema: "outreach");

            migrationBuilder.DropTable(
                name: "exclusion_addresses",
                schema: "outreach");

            migrationBuilder.DropTable(
                name: "exclusion_zones",
                schema: "outreach");

            migrationBuilder.DropTable(
                name: "follow_ups",
                schema: "outreach");

            migrationBuilder.DropTable(
                name: "reports",
                schema: "outreach");

            migrationBuilder.DropTable(
                name: "settings",
                schema: "outreach");

            migrationBuilder.DropTable(
                name: "skips",
                schema: "outreach");

            migrationBuilder.DropTable(
                name: "walk_pieces",
                schema: "outreach");

            migrationBuilder.DropTable(
                name: "walkers",
                schema: "outreach");

            migrationBuilder.DropTable(
                name: "routes",
                schema: "outreach");

            migrationBuilder.DropTable(
                name: "walks",
                schema: "outreach");

            migrationBuilder.DropTable(
                name: "days",
                schema: "outreach");
        }
    }
}

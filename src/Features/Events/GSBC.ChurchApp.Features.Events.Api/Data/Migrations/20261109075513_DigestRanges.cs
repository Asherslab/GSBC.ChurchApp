using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Events.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DigestRanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_digests",
                schema: "events",
                table: "digests");

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                schema: "events",
                table: "digests",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<DateOnly>(
                name: "end",
                schema: "events",
                table: "digests",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "range",
                schema: "events",
                table: "digests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE events.digests SET
                    "end" = start + (7 * GREATEST(weeks, 1) - 1),
                    range = CASE weeks WHEN 1 THEN 1 WHEN 2 THEN 0 ELSE 3 END;
                """);

            migrationBuilder.DropColumn(
                name: "weeks",
                schema: "events",
                table: "digests");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "events",
                table: "digests",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "end",
                schema: "events",
                table: "digests",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldDefaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddPrimaryKey(
                name: "pk_digests",
                schema: "events",
                table: "digests",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_digests_start",
                schema: "events",
                table: "digests",
                column: "start");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_digests",
                schema: "events",
                table: "digests");

            migrationBuilder.DropIndex(
                name: "ix_digests_start",
                schema: "events",
                table: "digests");

            migrationBuilder.Sql("""
                DELETE FROM events.digests older USING events.digests newer
                    WHERE older.start = newer.start AND (older.updated_at, older.id) < (newer.updated_at, newer.id);
                UPDATE events.digests SET range = LEAST(3, GREATEST(1, ("end" - start + 1) / 7));
                """);

            migrationBuilder.DropColumn(
                name: "id",
                schema: "events",
                table: "digests");

            migrationBuilder.DropColumn(
                name: "end",
                schema: "events",
                table: "digests");

            migrationBuilder.RenameColumn(
                name: "range",
                schema: "events",
                table: "digests",
                newName: "weeks");

            migrationBuilder.AddPrimaryKey(
                name: "pk_digests",
                schema: "events",
                table: "digests",
                column: "start");
        }
    }
}

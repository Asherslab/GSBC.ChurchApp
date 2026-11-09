using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Sermons.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class StandardServicesNoSpeakerAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH spellings(spelling, name) AS (VALUES ('sunday life groups', 'Sunday Life Groups'), ('sunday life group', 'Sunday Life Groups'), ('life groups', 'Sunday Life Groups'), ('sunday morning', 'Sunday Morning'), ('sunday am', 'Sunday Morning'), ('sun am', 'Sunday Morning'), ('sunday evening', 'Sunday Evening'), ('sunday night', 'Sunday Evening'), ('sunday pm', 'Sunday Evening'), ('sun pm', 'Sunday Evening'), ('wednesday night', 'Wednesday Night'), ('wednesday evening', 'Wednesday Night'), ('wednesday pm', 'Wednesday Night'), ('wednesday', 'Wednesday Night'), ('wed pm', 'Wednesday Night'), ('wed', 'Wednesday Night'))
                UPDATE sermons.sermons AS sermon SET source_service = spellings.name FROM spellings
                WHERE split_part(lower(regexp_replace(btrim(sermon.source_service), '\s+', ' ', 'g')), ' · ', 1) = spellings.spelling;
                """);

            migrationBuilder.Sql("""
                WITH spellings(spelling, name) AS (VALUES ('sunday life groups', 'Sunday Life Groups'), ('sunday life group', 'Sunday Life Groups'), ('life groups', 'Sunday Life Groups'), ('sunday morning', 'Sunday Morning'), ('sunday am', 'Sunday Morning'), ('sun am', 'Sunday Morning'), ('sunday evening', 'Sunday Evening'), ('sunday night', 'Sunday Evening'), ('sunday pm', 'Sunday Evening'), ('sun pm', 'Sunday Evening'), ('wednesday night', 'Wednesday Night'), ('wednesday evening', 'Wednesday Night'), ('wednesday pm', 'Wednesday Night'), ('wednesday', 'Wednesday Night'), ('wed pm', 'Wednesday Night'), ('wed', 'Wednesday Night'))
                UPDATE sermons.sermons AS sermon SET service = spellings.name FROM spellings
                WHERE split_part(lower(regexp_replace(btrim(sermon.service), '\s+', ' ', 'g')), ' · ', 1) = spellings.spelling;
                """);

            migrationBuilder.Sql("UPDATE sermons.sermons SET service = NULL WHERE service = source_service;");

            migrationBuilder.DropTable(
                name: "speaker_aliases",
                schema: "sermons");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "speaker_aliases",
                schema: "sermons",
                columns: table => new
                {
                    alias = table.Column<string>(type: "text", nullable: false),
                    speaker_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_speaker_aliases", x => x.alias);
                });

            migrationBuilder.CreateIndex(
                name: "ix_speaker_aliases_speaker_id",
                schema: "sermons",
                table: "speaker_aliases",
                column: "speaker_id");
        }
    }
}

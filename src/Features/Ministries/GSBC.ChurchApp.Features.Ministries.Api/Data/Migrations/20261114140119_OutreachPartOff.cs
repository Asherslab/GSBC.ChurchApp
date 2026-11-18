using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Ministries.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class OutreachPartOff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE ministries.ministries SET parts_off = array_append(parts_off, 'outreach') WHERE NOT 'outreach' = ANY(parts_off);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE ministries.ministries SET parts_off = array_remove(parts_off, 'outreach');");
        }
    }
}

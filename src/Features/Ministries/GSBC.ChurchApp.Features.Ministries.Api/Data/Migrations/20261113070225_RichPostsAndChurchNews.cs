using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Ministries.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RichPostsAndChurchNews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "church_news",
                schema: "ministries",
                table: "posts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "content",
                schema: "ministries",
                table: "posts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.Sql(PlainPosts.ToContent);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "church_news",
                schema: "ministries",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "content",
                schema: "ministries",
                table: "posts");
        }
    }
}

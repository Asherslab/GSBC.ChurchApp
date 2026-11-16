using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.News.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewsTimesTold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "times_told",
                schema: "news",
                table: "posts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE news.posts SET times_told = 1 WHERE told_at IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "times_told",
                schema: "news",
                table: "posts");
        }
    }
}

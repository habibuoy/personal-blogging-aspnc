using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalBlogging.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationalPropertyInTagOfArticle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tag_Articles_ArticleId",
                table: "Tag");

            migrationBuilder.DropIndex(
                name: "IX_Tag_ArticleId",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "ArticleId",
                table: "Tag");

            migrationBuilder.CreateTable(
                name: "ArticleTag",
                columns: table => new
                {
                    ArticlesId = table.Column<int>(type: "int", nullable: false),
                    TagsName = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleTag", x => new { x.ArticlesId, x.TagsName });
                    table.ForeignKey(
                        name: "FK_ArticleTag_Articles_ArticlesId",
                        column: x => x.ArticlesId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleTag_Tag_TagsName",
                        column: x => x.TagsName,
                        principalTable: "Tag",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleTag_TagsName",
                table: "ArticleTag",
                column: "TagsName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleTag");

            migrationBuilder.AddColumn<int>(
                name: "ArticleId",
                table: "Tag",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tag_ArticleId",
                table: "Tag",
                column: "ArticleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tag_Articles_ArticleId",
                table: "Tag",
                column: "ArticleId",
                principalTable: "Articles",
                principalColumn: "Id");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalBlogging.Migrations
{
    /// <inheritdoc />
    public partial class RenameTagTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArticleTag_Tag_TagsName",
                table: "ArticleTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tag",
                table: "Tag");

            migrationBuilder.RenameTable(
                name: "Tag",
                newName: "Tags");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tags",
                table: "Tags",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleTag_Tags_TagsName",
                table: "ArticleTag",
                column: "TagsName",
                principalTable: "Tags",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArticleTag_Tags_TagsName",
                table: "ArticleTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tags",
                table: "Tags");

            migrationBuilder.RenameTable(
                name: "Tags",
                newName: "Tag");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tag",
                table: "Tag",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleTag_Tag_TagsName",
                table: "ArticleTag",
                column: "TagsName",
                principalTable: "Tag",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

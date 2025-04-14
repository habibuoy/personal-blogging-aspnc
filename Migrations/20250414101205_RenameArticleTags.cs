using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalBlogging.Migrations
{
    /// <inheritdoc />
    public partial class RenameArticleTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Tags",
                table: "Articles",
                newName: "OldTags");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OldTags",
                table: "Articles",
                newName: "Tags");
        }
    }
}

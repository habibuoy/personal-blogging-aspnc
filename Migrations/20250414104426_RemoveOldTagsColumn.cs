using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalBlogging.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldTagsColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OldTags",
                table: "Articles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OldTags",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}

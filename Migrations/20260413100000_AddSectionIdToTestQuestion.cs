using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridAcademy.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionIdToTestQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "section_id",
                table: "test_questions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_test_questions_section_id",
                table: "test_questions",
                column: "section_id");

            migrationBuilder.AddForeignKey(
                name: "FK_test_questions_test_sections_section_id",
                table: "test_questions",
                column: "section_id",
                principalTable: "test_sections",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_test_questions_test_sections_section_id",
                table: "test_questions");

            migrationBuilder.DropIndex(
                name: "IX_test_questions_section_id",
                table: "test_questions");

            migrationBuilder.DropColumn(
                name: "section_id",
                table: "test_questions");
        }
    }
}

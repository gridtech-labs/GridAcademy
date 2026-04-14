using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridAcademy.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionModeAndNullableSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add question_mode column to tests (0 = GlobalBank, 1 = Manual)
            migrationBuilder.AddColumn<int>(
                name: "question_mode",
                table: "tests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Make subject_id nullable on test_sections
            // (Manual-mode sections are name-only; they don't need a subject)
            migrationBuilder.AlterColumn<int>(
                name: "subject_id",
                table: "test_sections",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "question_mode",
                table: "tests");

            // Revert subject_id to non-nullable (set a default of 0 for any nulls first)
            migrationBuilder.Sql(
                "UPDATE test_sections SET subject_id = 0 WHERE subject_id IS NULL");

            migrationBuilder.AlterColumn<int>(
                name: "subject_id",
                table: "test_sections",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}

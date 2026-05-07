using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridAcademy.Migrations
{
    /// <inheritdoc />
    public partial class RevertExamPageCategoryToExamCategoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: drop any existing FKs on these columns (all possible names),
            // then re-add pointing to exam_categories / exam_sub_categories.
            migrationBuilder.Sql("""
                ALTER TABLE exam_pages DROP CONSTRAINT IF EXISTS "FK_exam_pages_vl_domains_ExamCategoryId";
                ALTER TABLE exam_pages DROP CONSTRAINT IF EXISTS "FK_exam_pages_vl_video_categories_ExamSubCategoryId";
                ALTER TABLE exam_pages DROP CONSTRAINT IF EXISTS "FK_exam_pages_exam_categories_ExamCategoryId";
                ALTER TABLE exam_pages DROP CONSTRAINT IF EXISTS "FK_exam_pages_exam_sub_categories_ExamSubCategoryId";

                ALTER TABLE exam_pages
                    ADD CONSTRAINT "FK_exam_pages_exam_categories_ExamCategoryId"
                    FOREIGN KEY ("ExamCategoryId") REFERENCES exam_categories(id) ON DELETE SET NULL;

                ALTER TABLE exam_pages
                    ADD CONSTRAINT "FK_exam_pages_exam_sub_categories_ExamSubCategoryId"
                    FOREIGN KEY ("ExamSubCategoryId") REFERENCES exam_sub_categories(id) ON DELETE SET NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exam_pages_exam_categories_ExamCategoryId",
                table: "exam_pages");

            migrationBuilder.DropForeignKey(
                name: "FK_exam_pages_exam_sub_categories_ExamSubCategoryId",
                table: "exam_pages");

            migrationBuilder.AddForeignKey(
                name: "FK_exam_pages_vl_domains_ExamCategoryId",
                table: "exam_pages",
                column: "ExamCategoryId",
                principalTable: "vl_domains",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_exam_pages_vl_video_categories_ExamSubCategoryId",
                table: "exam_pages",
                column: "ExamSubCategoryId",
                principalTable: "vl_video_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

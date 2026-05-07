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
            migrationBuilder.DropForeignKey(
                name: "FK_exam_pages_vl_domains_ExamCategoryId",
                table: "exam_pages");

            migrationBuilder.DropForeignKey(
                name: "FK_exam_pages_vl_video_categories_ExamSubCategoryId",
                table: "exam_pages");

            migrationBuilder.AddForeignKey(
                name: "FK_exam_pages_exam_categories_ExamCategoryId",
                table: "exam_pages",
                column: "ExamCategoryId",
                principalTable: "exam_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_exam_pages_exam_sub_categories_ExamSubCategoryId",
                table: "exam_pages",
                column: "ExamSubCategoryId",
                principalTable: "exam_sub_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
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

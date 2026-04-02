using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridAcademy.Migrations
{
    public partial class AddManualScraperDraftPipeline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exam_notifications_exams_ExamId",
                table: "exam_notifications");

            migrationBuilder.AlterColumn<Guid>(
                name: "ExamId",
                table: "exam_notifications",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_exam_notifications_exams_ExamId",
                table: "exam_notifications",
                column: "ExamId",
                principalTable: "exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exam_notifications_exams_ExamId",
                table: "exam_notifications");

            migrationBuilder.AlterColumn<Guid>(
                name: "ExamId",
                table: "exam_notifications",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_exam_notifications_exams_ExamId",
                table: "exam_notifications",
                column: "ExamId",
                principalTable: "exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

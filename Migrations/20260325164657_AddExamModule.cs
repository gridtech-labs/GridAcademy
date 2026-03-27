using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GridAcademy.Migrations
{
    /// <inheritdoc />
    public partial class AddExamModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exam_levels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_levels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "exam_pages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Overview = table.Column<string>(type: "text", nullable: true),
                    Eligibility = table.Column<string>(type: "text", nullable: true),
                    Syllabus = table.Column<string>(type: "text", nullable: true),
                    ExamPattern = table.Column<string>(type: "text", nullable: true),
                    ImportantDates = table.Column<string>(type: "text", nullable: true),
                    AdmitCard = table.Column<string>(type: "text", nullable: true),
                    ResultInfo = table.Column<string>(type: "text", nullable: true),
                    CutOff = table.Column<string>(type: "text", nullable: true),
                    ConductingBody = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    OfficialWebsite = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NotificationUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExamLevelId = table.Column<int>(type: "integer", nullable: true),
                    ExamTypeId = table.Column<int>(type: "integer", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BannerUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ViewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    MetaTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    MetaDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_exam_pages_exam_levels_ExamLevelId",
                        column: x => x.ExamLevelId,
                        principalTable: "exam_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_exam_pages_exam_types_ExamTypeId",
                        column: x => x.ExamTypeId,
                        principalTable: "exam_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "exam_page_tests",
                columns: table => new
                {
                    ExamPageId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsFree = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_page_tests", x => new { x.ExamPageId, x.TestId });
                    table.ForeignKey(
                        name: "FK_exam_page_tests_exam_pages_ExamPageId",
                        column: x => x.ExamPageId,
                        principalTable: "exam_pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exam_page_tests_tests_TestId",
                        column: x => x.TestId,
                        principalTable: "tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_exam_page_tests_exam",
                table: "exam_page_tests",
                column: "ExamPageId");

            migrationBuilder.CreateIndex(
                name: "IX_exam_page_tests_TestId",
                table: "exam_page_tests",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_exam_pages_ExamTypeId",
                table: "exam_pages",
                column: "ExamTypeId");

            migrationBuilder.CreateIndex(
                name: "ix_exam_pages_featured",
                table: "exam_pages",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "ix_exam_pages_level",
                table: "exam_pages",
                column: "ExamLevelId");

            migrationBuilder.CreateIndex(
                name: "ix_exam_pages_slug_published",
                table: "exam_pages",
                column: "Slug",
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "ix_exam_pages_status",
                table: "exam_pages",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exam_page_tests");

            migrationBuilder.DropTable(
                name: "exam_pages");

            migrationBuilder.DropTable(
                name: "exam_levels");
        }
    }
}

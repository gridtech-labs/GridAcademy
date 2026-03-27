using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GridAcademy.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    instructions = table.Column<string>(type: "text", nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    passing_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    negative_marking_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    exam_type_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tests", x => x.id);
                    table.ForeignKey(
                        name: "FK_tests_exam_types_exam_type_id",
                        column: x => x.exam_type_id,
                        principalTable: "exam_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "test_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    available_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    available_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assigned_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_assignments_tests_test_id",
                        column: x => x.test_id,
                        principalTable: "tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_test_assignments_users_student_id",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_sections",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    test_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subject_id = table.Column<int>(type: "integer", nullable: false),
                    difficulty_level_id = table.Column<int>(type: "integer", nullable: true),
                    question_count = table.Column<int>(type: "integer", nullable: false),
                    marks_per_question = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    negative_marks_per_question = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_sections", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_sections_difficulty_levels_difficulty_level_id",
                        column: x => x.difficulty_level_id,
                        principalTable: "difficulty_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_test_sections_subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_test_sections_tests_test_id",
                        column: x => x.test_id,
                        principalTable: "tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_seconds_used = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_marks_obtained = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    total_marks_possible = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    is_passed = table.Column<bool>(type: "boolean", nullable: true),
                    violation_log = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_attempts", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_attempts_test_assignments_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "test_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attempt_answers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    selected_option_ids = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    numerical_value = table.Column<decimal>(type: "numeric(12,4)", nullable: true),
                    is_clear = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    saved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attempt_answers", x => x.id);
                    table.ForeignKey(
                        name: "FK_attempt_answers_test_attempts_attempt_id",
                        column: x => x.attempt_id,
                        principalTable: "test_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attempt_questions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_index = table.Column<int>(type: "integer", nullable: false),
                    section_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    display_order_in_section = table.Column<int>(type: "integer", nullable: false),
                    marks_for_correct = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    negative_marks = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    is_visited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_marked_for_review = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attempt_questions", x => x.id);
                    table.ForeignKey(
                        name: "FK_attempt_questions_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attempt_questions_test_attempts_attempt_id",
                        column: x => x.attempt_id,
                        principalTable: "test_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attempt_section_results",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_index = table.Column<int>(type: "integer", nullable: false),
                    section_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    total_questions = table.Column<int>(type: "integer", nullable: false),
                    attempted = table.Column<int>(type: "integer", nullable: false),
                    correct = table.Column<int>(type: "integer", nullable: false),
                    incorrect = table.Column<int>(type: "integer", nullable: false),
                    unattempted = table.Column<int>(type: "integer", nullable: false),
                    marks_obtained = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    max_marks = table.Column<decimal>(type: "numeric(8,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attempt_section_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_attempt_section_results_test_attempts_attempt_id",
                        column: x => x.attempt_id,
                        principalTable: "test_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attempt_answers_attempt_question",
                table: "attempt_answers",
                columns: new[] { "attempt_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attempt_questions_attempt_order",
                table: "attempt_questions",
                columns: new[] { "attempt_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_attempt_questions_attempt_question",
                table: "attempt_questions",
                columns: new[] { "attempt_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attempt_questions_question_id",
                table: "attempt_questions",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_attempt_section_results_attempt_id",
                table: "attempt_section_results",
                column: "attempt_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_assignments_student_id",
                table: "test_assignments",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_assignments_test_student",
                table: "test_assignments",
                columns: new[] { "test_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_test_attempts_assignment_num",
                table: "test_attempts",
                columns: new[] { "assignment_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_test_attempts_status",
                table: "test_attempts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_test_attempts_student_test",
                table: "test_attempts",
                columns: new[] { "student_id", "test_id" });

            migrationBuilder.CreateIndex(
                name: "IX_test_sections_difficulty_level_id",
                table: "test_sections",
                column: "difficulty_level_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_sections_subject_id",
                table: "test_sections",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_sections_test_sort",
                table: "test_sections",
                columns: new[] { "test_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_tests_exam_type_id",
                table: "tests",
                column: "exam_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_tests_status",
                table: "tests",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attempt_answers");

            migrationBuilder.DropTable(
                name: "attempt_questions");

            migrationBuilder.DropTable(
                name: "attempt_section_results");

            migrationBuilder.DropTable(
                name: "test_sections");

            migrationBuilder.DropTable(
                name: "test_attempts");

            migrationBuilder.DropTable(
                name: "test_assignments");

            migrationBuilder.DropTable(
                name: "tests");
        }
    }
}

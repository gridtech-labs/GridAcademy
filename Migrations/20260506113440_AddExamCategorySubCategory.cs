using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GridAcademy.Migrations
{
    /// <inheritdoc />
    public partial class AddExamCategorySubCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_questions_exam_types_exam_type_id",
                table: "questions");

            migrationBuilder.DropForeignKey(
                name: "FK_tests_exam_types_exam_type_id",
                table: "tests");

            migrationBuilder.DropIndex(
                name: "ix_tests_exam_type_id",
                table: "tests");

            migrationBuilder.DropColumn(
                name: "exam_type_id",
                table: "tests");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "exam_pages");

            migrationBuilder.RenameColumn(
                name: "exam_type_id",
                table: "questions",
                newName: "ExamTypeId");

            migrationBuilder.RenameIndex(
                name: "ix_questions_exam_type_id",
                table: "questions",
                newName: "IX_questions_ExamTypeId");

            // question_mode, subject_id nullable, section_id, and related index/FK
            // were already applied via manual migrations — use IF NOT EXISTS to be safe
            migrationBuilder.Sql("""
                ALTER TABLE tests ADD COLUMN IF NOT EXISTS question_mode integer NOT NULL DEFAULT 0;
                ALTER TABLE test_sections ALTER COLUMN subject_id DROP NOT NULL;
                ALTER TABLE test_questions ADD COLUMN IF NOT EXISTS section_id integer;
                CREATE INDEX IF NOT EXISTS "IX_test_questions_section_id" ON test_questions (section_id);
                ALTER TABLE test_questions
                    DROP CONSTRAINT IF EXISTS "FK_test_questions_test_sections_section_id";
                ALTER TABLE test_questions
                    ADD CONSTRAINT "FK_test_questions_test_sections_section_id"
                    FOREIGN KEY (section_id) REFERENCES test_sections(id) ON DELETE SET NULL;
                """);

            migrationBuilder.AddColumn<bool>(
                name: "instructions_acknowledged",
                table: "test_attempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "ExamTypeId",
                table: "questions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "ExamCategoryId",
                table: "exam_pages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExamSubCategoryId",
                table: "exam_pages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "exam_categories",
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
                    table.PrimaryKey("PK_exam_categories", x => x.id);
                });

            // system_roles may already exist from a prior migration — create conditionally
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS system_roles (
                    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    name character varying(50) NOT NULL,
                    display_name character varying(100) NOT NULL,
                    description text,
                    color character varying(20),
                    is_system boolean NOT NULL DEFAULT false,
                    is_active boolean NOT NULL DEFAULT true,
                    sort_order integer NOT NULL DEFAULT 0,
                    created_at timestamp with time zone NOT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_system_roles_name ON system_roles (name);
                """);

            migrationBuilder.CreateTable(
                name: "exam_sub_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    exam_category_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_sub_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_exam_sub_categories_exam_categories_exam_category_id",
                        column: x => x.exam_category_id,
                        principalTable: "exam_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // user_role_maps may already exist — create conditionally
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS user_role_maps (
                    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                    role_id integer NOT NULL REFERENCES system_roles(id) ON DELETE CASCADE,
                    assigned_at timestamp with time zone NOT NULL,
                    assigned_by uuid
                );
                CREATE INDEX IF NOT EXISTS "IX_user_role_maps_role_id" ON user_role_maps (role_id);
                CREATE UNIQUE INDEX IF NOT EXISTS ix_user_role_maps_user_role ON user_role_maps (user_id, role_id);
                """);

            migrationBuilder.CreateIndex(
                name: "ix_exam_pages_category",
                table: "exam_pages",
                column: "ExamCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_exam_pages_ExamSubCategoryId",
                table: "exam_pages",
                column: "ExamSubCategoryId");

            migrationBuilder.CreateIndex(
                name: "ix_exam_sub_categories_category_id",
                table: "exam_sub_categories",
                column: "exam_category_id");

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

            migrationBuilder.AddForeignKey(
                name: "FK_questions_exam_types_ExamTypeId",
                table: "questions",
                column: "ExamTypeId",
                principalTable: "exam_types",
                principalColumn: "id");

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

            migrationBuilder.DropForeignKey(
                name: "FK_questions_exam_types_ExamTypeId",
                table: "questions");

            migrationBuilder.DropForeignKey(
                name: "FK_test_questions_test_sections_section_id",
                table: "test_questions");

            migrationBuilder.DropTable(
                name: "exam_sub_categories");

            migrationBuilder.DropTable(
                name: "user_role_maps");

            migrationBuilder.DropTable(
                name: "exam_categories");

            migrationBuilder.DropTable(
                name: "system_roles");

            migrationBuilder.DropIndex(
                name: "IX_test_questions_section_id",
                table: "test_questions");

            migrationBuilder.DropIndex(
                name: "ix_exam_pages_category",
                table: "exam_pages");

            migrationBuilder.DropIndex(
                name: "IX_exam_pages_ExamSubCategoryId",
                table: "exam_pages");

            migrationBuilder.DropColumn(
                name: "question_mode",
                table: "tests");

            migrationBuilder.DropColumn(
                name: "section_id",
                table: "test_questions");

            migrationBuilder.DropColumn(
                name: "instructions_acknowledged",
                table: "test_attempts");

            migrationBuilder.DropColumn(
                name: "ExamCategoryId",
                table: "exam_pages");

            migrationBuilder.DropColumn(
                name: "ExamSubCategoryId",
                table: "exam_pages");

            migrationBuilder.RenameColumn(
                name: "ExamTypeId",
                table: "questions",
                newName: "exam_type_id");

            migrationBuilder.RenameIndex(
                name: "IX_questions_ExamTypeId",
                table: "questions",
                newName: "ix_questions_exam_type_id");

            migrationBuilder.AddColumn<int>(
                name: "exam_type_id",
                table: "tests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "subject_id",
                table: "test_sections",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "exam_type_id",
                table: "questions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "exam_pages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_tests_exam_type_id",
                table: "tests",
                column: "exam_type_id");

            migrationBuilder.AddForeignKey(
                name: "FK_questions_exam_types_exam_type_id",
                table: "questions",
                column: "exam_type_id",
                principalTable: "exam_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tests_exam_types_exam_type_id",
                table: "tests",
                column: "exam_type_id",
                principalTable: "exam_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

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
            // Fully idempotent: every step uses IF EXISTS / IF NOT EXISTS so the
            // migration can be retried safely regardless of partial prior runs.
            migrationBuilder.Sql("""
                -- 1. Drop exam_type_id FK + index + column from tests (if still present)
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_name='tests' AND column_name='exam_type_id') THEN
                        ALTER TABLE tests DROP CONSTRAINT IF EXISTS "FK_tests_exam_types_exam_type_id";
                        DROP INDEX IF EXISTS ix_tests_exam_type_id;
                        ALTER TABLE tests DROP COLUMN IF EXISTS exam_type_id;
                    END IF;
                END; $$;

                -- 2. Drop legacy Category string column from exam_pages
                ALTER TABLE exam_pages DROP COLUMN IF EXISTS "Category";

                -- 3. Rename exam_type_id → ExamTypeId on questions (if not already renamed)
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_name='questions' AND column_name='exam_type_id') THEN
                        ALTER TABLE questions RENAME COLUMN exam_type_id TO "ExamTypeId";
                    END IF;
                END; $$;

                -- 4. Rename index on questions
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_indexes
                               WHERE tablename='questions' AND indexname='ix_questions_exam_type_id') THEN
                        ALTER INDEX ix_questions_exam_type_id RENAME TO "IX_questions_ExamTypeId";
                    END IF;
                END; $$;

                -- 5. Make ExamTypeId nullable on questions
                ALTER TABLE questions ALTER COLUMN "ExamTypeId" DROP NOT NULL;

                -- 6. Already-applied manual-migration columns
                ALTER TABLE tests         ADD COLUMN IF NOT EXISTS question_mode             integer NOT NULL DEFAULT 0;
                ALTER TABLE test_sections ALTER COLUMN subject_id DROP NOT NULL;
                ALTER TABLE test_questions ADD COLUMN IF NOT EXISTS section_id               integer;
                ALTER TABLE test_attempts  ADD COLUMN IF NOT EXISTS instructions_acknowledged boolean NOT NULL DEFAULT false;

                -- 7. section_id index + FK
                CREATE INDEX IF NOT EXISTS "IX_test_questions_section_id" ON test_questions (section_id);
                ALTER TABLE test_questions DROP CONSTRAINT IF EXISTS "FK_test_questions_test_sections_section_id";
                ALTER TABLE test_questions
                    ADD CONSTRAINT "FK_test_questions_test_sections_section_id"
                    FOREIGN KEY (section_id) REFERENCES test_sections(id) ON DELETE SET NULL;

                -- 8. exam_categories table
                CREATE TABLE IF NOT EXISTS exam_categories (
                    id         integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    name       character varying(150) NOT NULL,
                    is_active  boolean NOT NULL DEFAULT true,
                    sort_order integer NOT NULL DEFAULT 0
                );

                -- 9. system_roles table
                CREATE TABLE IF NOT EXISTS system_roles (
                    id           integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    name         character varying(50)  NOT NULL,
                    display_name character varying(100) NOT NULL,
                    description  text,
                    color        character varying(20),
                    is_system    boolean NOT NULL DEFAULT false,
                    is_active    boolean NOT NULL DEFAULT true,
                    sort_order   integer NOT NULL DEFAULT 0,
                    created_at   timestamp with time zone NOT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_system_roles_name ON system_roles (name);

                -- 10. exam_sub_categories table
                CREATE TABLE IF NOT EXISTS exam_sub_categories (
                    id               integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    exam_category_id integer NOT NULL,
                    name             character varying(150) NOT NULL,
                    is_active        boolean NOT NULL DEFAULT true,
                    sort_order       integer NOT NULL DEFAULT 0
                );
                ALTER TABLE exam_sub_categories DROP CONSTRAINT IF EXISTS "FK_exam_sub_categories_exam_categories_exam_category_id";
                ALTER TABLE exam_sub_categories
                    ADD CONSTRAINT "FK_exam_sub_categories_exam_categories_exam_category_id"
                    FOREIGN KEY (exam_category_id) REFERENCES exam_categories(id) ON DELETE CASCADE;
                CREATE INDEX IF NOT EXISTS ix_exam_sub_categories_category_id ON exam_sub_categories (exam_category_id);

                -- 11. user_role_maps table
                CREATE TABLE IF NOT EXISTS user_role_maps (
                    id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    user_id     uuid    NOT NULL REFERENCES users(id)         ON DELETE CASCADE,
                    role_id     integer NOT NULL REFERENCES system_roles(id)  ON DELETE CASCADE,
                    assigned_at timestamp with time zone NOT NULL,
                    assigned_by uuid
                );
                CREATE INDEX        IF NOT EXISTS "IX_user_role_maps_role_id"    ON user_role_maps (role_id);
                CREATE UNIQUE INDEX IF NOT EXISTS ix_user_role_maps_user_role    ON user_role_maps (user_id, role_id);

                -- 12. ExamCategoryId + ExamSubCategoryId columns on exam_pages
                ALTER TABLE exam_pages ADD COLUMN IF NOT EXISTS "ExamCategoryId"    integer;
                ALTER TABLE exam_pages ADD COLUMN IF NOT EXISTS "ExamSubCategoryId" integer;

                -- 13. Indexes on exam_pages
                CREATE INDEX IF NOT EXISTS ix_exam_pages_category          ON exam_pages ("ExamCategoryId");
                CREATE INDEX IF NOT EXISTS "IX_exam_pages_ExamSubCategoryId" ON exam_pages ("ExamSubCategoryId");

                -- 14. FKs on exam_pages
                ALTER TABLE exam_pages DROP CONSTRAINT IF EXISTS "FK_exam_pages_exam_categories_ExamCategoryId";
                ALTER TABLE exam_pages
                    ADD CONSTRAINT "FK_exam_pages_exam_categories_ExamCategoryId"
                    FOREIGN KEY ("ExamCategoryId") REFERENCES exam_categories(id) ON DELETE SET NULL;

                ALTER TABLE exam_pages DROP CONSTRAINT IF EXISTS "FK_exam_pages_exam_sub_categories_ExamSubCategoryId";
                ALTER TABLE exam_pages
                    ADD CONSTRAINT "FK_exam_pages_exam_sub_categories_ExamSubCategoryId"
                    FOREIGN KEY ("ExamSubCategoryId") REFERENCES exam_sub_categories(id) ON DELETE SET NULL;

                -- 15. Re-add ExamTypeId FK on questions (optional / soft)
                ALTER TABLE questions DROP CONSTRAINT IF EXISTS "FK_questions_exam_types_ExamTypeId";
                ALTER TABLE questions DROP CONSTRAINT IF EXISTS "FK_questions_exam_types_exam_type_id";
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_name='questions' AND column_name='"ExamTypeId"')
                    OR EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_name='questions' AND column_name='ExamTypeId') THEN
                        ALTER TABLE questions
                            ADD CONSTRAINT "FK_questions_exam_types_ExamTypeId"
                            FOREIGN KEY ("ExamTypeId") REFERENCES exam_types(id);
                    END IF;
                END; $$;
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

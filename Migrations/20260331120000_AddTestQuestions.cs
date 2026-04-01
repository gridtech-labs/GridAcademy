using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GridAcademy.Migrations
{
    /// <inheritdoc />
    public partial class AddTestQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "test_questions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    test_id     = table.Column<Guid>(type: "uuid",       nullable: false),
                    question_id = table.Column<Guid>(type: "uuid",       nullable: false),
                    sort_order  = table.Column<int> (type: "integer",    nullable: false, defaultValue: 0),
                    added_at    = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_questions", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_questions_tests_test_id",
                        column: x => x.test_id,
                        principalTable: "tests",
                        principalColumn: "id",           // ← lowercase, matches tests.id
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_test_questions_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",           // ← lowercase, matches questions.id
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_test_questions_test_id",
                table: "test_questions",
                column: "test_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_questions_question_id",
                table: "test_questions",
                column: "question_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "test_questions");
        }
    }
}

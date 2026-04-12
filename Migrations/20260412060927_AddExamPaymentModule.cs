using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GridAcademy.Migrations
{
    /// <inheritdoc />
    public partial class AddExamPaymentModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_hashes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hash_value = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_hashes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ExamOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OfferType = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxDiscountAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    ExamPageId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaxUses = table.Column<int>(type: "integer", nullable: true),
                    UsedCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamOffers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "exams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    level = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exams", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "test_questions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    test_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    added_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_questions", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_questions_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_test_questions_tests_test_id",
                        column: x => x.test_id,
                        principalTable: "tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamPageId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    FinalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    GstAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    OfferId = table.Column<int>(type: "integer", nullable: true),
                    OfferCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RazorpayOrderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RazorpayPaymentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RazorpaySignature = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BookingRef = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamOrders_ExamOffers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "ExamOffers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamOrders_exam_pages_ExamPageId",
                        column: x => x.ExamPageId,
                        principalTable: "exam_pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamOrders_users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exam_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    content_html = table.Column<string>(type: "text", nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notification_type = table.Column<int>(type: "integer", nullable: false),
                    important_dates = table.Column<string>(type: "jsonb", nullable: true),
                    source_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    canonical_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    meta_title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    meta_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    is_ai_processed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ai_processed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_exam_notifications_exams_exam_id",
                        column: x => x.exam_id,
                        principalTable: "exams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ExamAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamPageId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAccesses_ExamOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "ExamOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAccesses_exam_pages_ExamPageId",
                        column: x => x.ExamPageId,
                        principalTable: "exam_pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAccesses_users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamOrderTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Event = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RazorpayPaymentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RazorpayOrderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: true),
                    RawPayload = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamOrderTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamOrderTransactions_ExamOrders_ExamOrderId",
                        column: x => x.ExamOrderId,
                        principalTable: "ExamOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "content_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_html = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    ExamNotificationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_content_versions_exam_notifications_ExamNotificationId",
                        column: x => x.ExamNotificationId,
                        principalTable: "exam_notifications",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_content_hashes_hash_value",
                table: "content_hashes",
                column: "hash_value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_content_versions_entity",
                table: "content_versions",
                columns: new[] { "entity_type", "entity_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_content_versions_ExamNotificationId",
                table: "content_versions",
                column: "ExamNotificationId");

            migrationBuilder.CreateIndex(
                name: "ix_exam_notifications_exam_id",
                table: "exam_notifications",
                column: "exam_id");

            migrationBuilder.CreateIndex(
                name: "ix_exam_notifications_is_ai_processed",
                table: "exam_notifications",
                column: "is_ai_processed");

            migrationBuilder.CreateIndex(
                name: "ix_exam_notifications_published_at",
                table: "exam_notifications",
                column: "published_at");

            migrationBuilder.CreateIndex(
                name: "ix_exam_notifications_slug",
                table: "exam_notifications",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exam_notifications_status",
                table: "exam_notifications",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_exam_notifications_type",
                table: "exam_notifications",
                column: "notification_type");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAccesses_ExamPageId",
                table: "ExamAccesses",
                column: "ExamPageId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAccesses_OrderId",
                table: "ExamAccesses",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamAccesses_StudentId",
                table: "ExamAccesses",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamOrders_ExamPageId",
                table: "ExamOrders",
                column: "ExamPageId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamOrders_OfferId",
                table: "ExamOrders",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamOrders_StudentId",
                table: "ExamOrders",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamOrderTransactions_ExamOrderId",
                table: "ExamOrderTransactions",
                column: "ExamOrderId");

            migrationBuilder.CreateIndex(
                name: "ix_exams_category_level",
                table: "exams",
                columns: new[] { "category", "level" });

            migrationBuilder.CreateIndex(
                name: "ix_exams_slug",
                table: "exams",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_test_questions_question_id",
                table: "test_questions",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_questions_test_id",
                table: "test_questions",
                column: "test_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_hashes");

            migrationBuilder.DropTable(
                name: "content_versions");

            migrationBuilder.DropTable(
                name: "ExamAccesses");

            migrationBuilder.DropTable(
                name: "ExamOrderTransactions");

            migrationBuilder.DropTable(
                name: "test_questions");

            migrationBuilder.DropTable(
                name: "exam_notifications");

            migrationBuilder.DropTable(
                name: "ExamOrders");

            migrationBuilder.DropTable(
                name: "exams");

            migrationBuilder.DropTable(
                name: "ExamOffers");
        }
    }
}

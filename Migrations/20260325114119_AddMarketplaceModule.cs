using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GridAcademy.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "mp_cms_banners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LinkUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mp_cms_banners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mp_otp_sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Contact = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OtpCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mp_otp_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mp_promo_codes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DiscountType = table.Column<int>(type: "integer", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    MaxDiscount = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsageLimit = table.Column<int>(type: "integer", nullable: true),
                    UsedCount = table.Column<int>(type: "integer", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mp_promo_codes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mp_providers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstituteName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PanNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BankAccountEncrypted = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IfscCode = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    AccountHolderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AdminNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AgreedToTerms = table.Column<bool>(type: "boolean", nullable: false),
                    AgreedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mp_providers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mp_providers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mp_payouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    RazorpayTransferId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InitiatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mp_payouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mp_payouts_mp_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "mp_providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mp_test_series",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<int>(type: "integer", nullable: false),
                    ExamTypeId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "character varying(350)", maxLength: 350, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FullDescription = table.Column<string>(type: "text", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SeriesType = table.Column<int>(type: "integer", nullable: false),
                    PriceInr = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    IsFirstTestFree = table.Column<bool>(type: "boolean", nullable: false),
                    Language = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    PurchaseCount = table.Column<int>(type: "integer", nullable: false),
                    AvgRating = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mp_test_series", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mp_test_series_exam_types_ExamTypeId",
                        column: x => x.ExamTypeId,
                        principalTable: "exam_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mp_test_series_mp_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "mp_providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mp_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountInr = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    GstAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    BookingFee = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    PromoCodeApplied = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DiscountApplied = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    RazorpayOrderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RazorpayPaymentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BookingRef = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mp_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mp_orders_mp_test_series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "mp_test_series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mp_orders_users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mp_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mp_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mp_reviews_mp_test_series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "mp_test_series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mp_reviews_users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mp_series_tests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsFreePreview = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mp_series_tests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mp_series_tests_mp_test_series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "mp_test_series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mp_series_tests_tests_TestId",
                        column: x => x.TestId,
                        principalTable: "tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mp_commissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<int>(type: "integer", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    PlatformPct = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    PlatformAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ProviderPct = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ProviderAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PayoutId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mp_commissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mp_commissions_mp_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "mp_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mp_commissions_mp_payouts_PayoutId",
                        column: x => x.PayoutId,
                        principalTable: "mp_payouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_mp_commissions_mp_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "mp_providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mp_entitlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mp_entitlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mp_entitlements_mp_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "mp_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mp_entitlements_mp_test_series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "mp_test_series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mp_entitlements_users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mp_commissions_OrderId",
                table: "mp_commissions",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mp_commissions_PayoutId",
                table: "mp_commissions",
                column: "PayoutId");

            migrationBuilder.CreateIndex(
                name: "ix_mp_commissions_provider",
                table: "mp_commissions",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "ix_mp_commissions_status",
                table: "mp_commissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_mp_entitlements_OrderId",
                table: "mp_entitlements",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mp_entitlements_SeriesId",
                table: "mp_entitlements",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "ix_mp_entitlements_student_series",
                table: "mp_entitlements",
                columns: new[] { "StudentId", "SeriesId" });

            migrationBuilder.CreateIndex(
                name: "ix_mp_orders_booking_ref",
                table: "mp_orders",
                column: "BookingRef",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mp_orders_razorpay",
                table: "mp_orders",
                column: "RazorpayOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_mp_orders_SeriesId",
                table: "mp_orders",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "ix_mp_orders_student",
                table: "mp_orders",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "ix_mp_otp_contact",
                table: "mp_otp_sessions",
                column: "Contact");

            migrationBuilder.CreateIndex(
                name: "ix_mp_payouts_provider",
                table: "mp_payouts",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "ix_mp_promo_code_unique",
                table: "mp_promo_codes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mp_providers_user_id",
                table: "mp_providers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mp_reviews_SeriesId",
                table: "mp_reviews",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "ix_mp_reviews_student_series",
                table: "mp_reviews",
                columns: new[] { "StudentId", "SeriesId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mp_series_tests_TestId",
                table: "mp_series_tests",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "ix_mp_series_tests_unique",
                table: "mp_series_tests",
                columns: new[] { "SeriesId", "TestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mp_series_exam_type",
                table: "mp_test_series",
                column: "ExamTypeId");

            migrationBuilder.CreateIndex(
                name: "ix_mp_series_provider",
                table: "mp_test_series",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "ix_mp_series_slug_published",
                table: "mp_test_series",
                column: "Slug",
                unique: true,
                filter: "\"Status\" = 2");

            migrationBuilder.CreateIndex(
                name: "ix_mp_series_status",
                table: "mp_test_series",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mp_cms_banners");

            migrationBuilder.DropTable(
                name: "mp_commissions");

            migrationBuilder.DropTable(
                name: "mp_entitlements");

            migrationBuilder.DropTable(
                name: "mp_otp_sessions");

            migrationBuilder.DropTable(
                name: "mp_promo_codes");

            migrationBuilder.DropTable(
                name: "mp_reviews");

            migrationBuilder.DropTable(
                name: "mp_series_tests");

            migrationBuilder.DropTable(
                name: "mp_payouts");

            migrationBuilder.DropTable(
                name: "mp_orders");

            migrationBuilder.DropTable(
                name: "mp_test_series");

            migrationBuilder.DropTable(
                name: "mp_providers");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "users");
        }
    }
}

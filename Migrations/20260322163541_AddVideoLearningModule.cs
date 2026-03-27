using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GridAcademy.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoLearningModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vl_domains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vl_domains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vl_sales_channels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApiKeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vl_sales_channels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vl_learning_paths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DomainId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ThumbnailPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vl_learning_paths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vl_learning_paths_vl_domains_DomainId",
                        column: x => x.DomainId,
                        principalTable: "vl_domains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vl_programs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DomainId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ThumbnailPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vl_programs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vl_programs_vl_domains_DomainId",
                        column: x => x.DomainId,
                        principalTable: "vl_domains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vl_video_categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DomainId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vl_video_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vl_video_categories_vl_domains_DomainId",
                        column: x => x.DomainId,
                        principalTable: "vl_domains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vl_coupons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DiscountType = table.Column<int>(type: "integer", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    MaxDiscountInr = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    MaxDiscountUsd = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    UsageLimit = table.Column<int>(type: "integer", nullable: true),
                    UsedCount = table.Column<int>(type: "integer", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vl_coupons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vl_coupons_vl_programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "vl_programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "vl_program_learning_paths",
                columns: table => new
                {
                    LearningPathId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vl_program_learning_paths", x => new { x.ProgramId, x.LearningPathId });
                    table.ForeignKey(
                        name: "FK_vl_program_learning_paths_vl_learning_paths_LearningPathId",
                        column: x => x.LearningPathId,
                        principalTable: "vl_learning_paths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vl_program_learning_paths_vl_programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "vl_programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vl_program_pricing_plans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PriceInr = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    PriceUsd = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    OriginalPriceInr = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    OriginalPriceUsd = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    ValidityDays = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vl_program_pricing_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vl_program_pricing_plans_vl_programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "vl_programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vl_videos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DomainId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ThumbnailPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    IsFreePreview = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vl_videos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vl_videos_vl_domains_DomainId",
                        column: x => x.DomainId,
                        principalTable: "vl_domains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vl_videos_vl_video_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "vl_video_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vl_channel_program_prices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChannelId = table.Column<int>(type: "integer", nullable: false),
                    PricingPlanId = table.Column<int>(type: "integer", nullable: false),
                    OverridePriceInr = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    OverridePriceUsd = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vl_channel_program_prices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vl_channel_program_prices_vl_program_pricing_plans_PricingP~",
                        column: x => x.PricingPlanId,
                        principalTable: "vl_program_pricing_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vl_channel_program_prices_vl_sales_channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "vl_sales_channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vl_enrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    PricingPlanId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AmountPaidInr = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    AmountPaidUsd = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    CouponCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DiscountApplied = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    ChannelId = table.Column<int>(type: "integer", nullable: true),
                    EnrolledAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vl_enrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vl_enrollments_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vl_enrollments_vl_program_pricing_plans_PricingPlanId",
                        column: x => x.PricingPlanId,
                        principalTable: "vl_program_pricing_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vl_enrollments_vl_programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "vl_programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vl_enrollments_vl_sales_channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "vl_sales_channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "vl_learning_path_videos",
                columns: table => new
                {
                    LearningPathId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vl_learning_path_videos", x => new { x.LearningPathId, x.VideoId });
                    table.ForeignKey(
                        name: "FK_vl_learning_path_videos_vl_learning_paths_LearningPathId",
                        column: x => x.LearningPathId,
                        principalTable: "vl_learning_paths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vl_learning_path_videos_vl_videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "vl_videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vl_video_progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    WatchedSeconds = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vl_video_progress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vl_video_progress_vl_enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "vl_enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vl_video_progress_vl_videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "vl_videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vl_channel_program_prices_PricingPlanId",
                table: "vl_channel_program_prices",
                column: "PricingPlanId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_cpp_channel_plan",
                table: "vl_channel_program_prices",
                columns: new[] { "ChannelId", "PricingPlanId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vl_coupons_code",
                table: "vl_coupons",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vl_coupons_program_id",
                table: "vl_coupons",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_vl_enrollments_ChannelId",
                table: "vl_enrollments",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_vl_enrollments_PricingPlanId",
                table: "vl_enrollments",
                column: "PricingPlanId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_enrollments_program_id",
                table: "vl_enrollments",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_enrollments_status",
                table: "vl_enrollments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_vl_enrollments_user_id",
                table: "vl_enrollments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_enrollments_user_program_active",
                table: "vl_enrollments",
                columns: new[] { "UserId", "ProgramId" },
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "ix_vl_lpv_video_id",
                table: "vl_learning_path_videos",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_learning_paths_domain_id",
                table: "vl_learning_paths",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_vl_program_learning_paths_LearningPathId",
                table: "vl_program_learning_paths",
                column: "LearningPathId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_ppp_program_id",
                table: "vl_program_pricing_plans",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_programs_domain_id",
                table: "vl_programs",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_programs_status",
                table: "vl_programs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_vl_sales_channels_api_key_hash",
                table: "vl_sales_channels",
                column: "ApiKeyHash");

            migrationBuilder.CreateIndex(
                name: "ix_vl_video_categories_domain_id",
                table: "vl_video_categories",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_vl_video_progress_VideoId",
                table: "vl_video_progress",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_vp_enrollment_id",
                table: "vl_video_progress",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_vp_enrollment_video",
                table: "vl_video_progress",
                columns: new[] { "EnrollmentId", "VideoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vl_videos_category_id",
                table: "vl_videos",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_videos_domain_id",
                table: "vl_videos",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_videos_status",
                table: "vl_videos",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vl_channel_program_prices");

            migrationBuilder.DropTable(
                name: "vl_coupons");

            migrationBuilder.DropTable(
                name: "vl_learning_path_videos");

            migrationBuilder.DropTable(
                name: "vl_program_learning_paths");

            migrationBuilder.DropTable(
                name: "vl_video_progress");

            migrationBuilder.DropTable(
                name: "vl_learning_paths");

            migrationBuilder.DropTable(
                name: "vl_enrollments");

            migrationBuilder.DropTable(
                name: "vl_videos");

            migrationBuilder.DropTable(
                name: "vl_program_pricing_plans");

            migrationBuilder.DropTable(
                name: "vl_sales_channels");

            migrationBuilder.DropTable(
                name: "vl_video_categories");

            migrationBuilder.DropTable(
                name: "vl_programs");

            migrationBuilder.DropTable(
                name: "vl_domains");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GridAcademy.Migrations
{
    /// <inheritdoc />
    public partial class RedesignLearningPathNodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vl_learning_path_items");

            migrationBuilder.DropTable(
                name: "vl_learning_path_modules");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "vl_learning_paths");

            migrationBuilder.CreateTable(
                name: "vl_learning_path_nodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LearningPathId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentNodeId = table.Column<int>(type: "integer", nullable: true),
                    NodeType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPreview = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vl_learning_path_nodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vl_learning_path_nodes_vl_learning_path_nodes_ParentNodeId",
                        column: x => x.ParentNodeId,
                        principalTable: "vl_learning_path_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vl_learning_path_nodes_vl_learning_paths_LearningPathId",
                        column: x => x.LearningPathId,
                        principalTable: "vl_learning_paths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vl_lpn_content_id",
                table: "vl_learning_path_nodes",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_lpn_learning_path_id",
                table: "vl_learning_path_nodes",
                column: "LearningPathId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_lpn_node_type",
                table: "vl_learning_path_nodes",
                column: "NodeType");

            migrationBuilder.CreateIndex(
                name: "ix_vl_lpn_parent_node_id",
                table: "vl_learning_path_nodes",
                column: "ParentNodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vl_learning_path_nodes");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "vl_learning_paths",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "vl_learning_path_modules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LearningPathId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vl_learning_path_modules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vl_learning_path_modules_vl_learning_paths_LearningPathId",
                        column: x => x.LearningPathId,
                        principalTable: "vl_learning_paths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vl_learning_path_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContentFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    LearningPathId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<int>(type: "integer", nullable: true),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContentType = table.Column<int>(type: "integer", nullable: false),
                    IsPreview = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TestId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vl_learning_path_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vl_learning_path_items_vl_content_files_ContentFileId",
                        column: x => x.ContentFileId,
                        principalTable: "vl_content_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_vl_learning_path_items_vl_learning_path_modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "vl_learning_path_modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vl_learning_path_items_vl_learning_paths_LearningPathId",
                        column: x => x.LearningPathId,
                        principalTable: "vl_learning_paths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vl_learning_path_items_vl_videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "vl_videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vl_learning_path_items_ContentFileId",
                table: "vl_learning_path_items",
                column: "ContentFileId");

            migrationBuilder.CreateIndex(
                name: "IX_vl_learning_path_items_VideoId",
                table: "vl_learning_path_items",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_lpi_learning_path_id",
                table: "vl_learning_path_items",
                column: "LearningPathId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_lpi_module_id",
                table: "vl_learning_path_items",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "ix_vl_lpm_learning_path_id",
                table: "vl_learning_path_modules",
                column: "LearningPathId");
        }
    }
}

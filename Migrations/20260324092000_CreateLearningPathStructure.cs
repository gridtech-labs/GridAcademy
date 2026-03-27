using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GridAcademy.Migrations
{
    /// <inheritdoc />
    public partial class CreateLearningPathStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop old junction table (replaced by VlLearningPathItem) — safe even if absent
            migrationBuilder.Sql("DROP TABLE IF EXISTS vl_learning_path_videos;");

            // 2. Add Type column to vl_learning_paths (0=Flat, 1=ModuleBased) — idempotent
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name='vl_learning_paths' AND column_name='Type'
                    ) THEN
                        ALTER TABLE vl_learning_paths ADD COLUMN ""Type"" integer NOT NULL DEFAULT 0;
                    END IF;
                END $$;");

            // 3. Create vl_content_files
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS vl_content_files (
                    ""Id""               uuid          NOT NULL,
                    ""DomainId""         integer       NOT NULL,
                    ""Title""            varchar(300)  NOT NULL,
                    ""Description""      text,
                    ""ContentType""      integer       NOT NULL,
                    ""FilePath""         varchar(500),
                    ""OriginalFileName"" varchar(255),
                    ""FileSizeBytes""    bigint        NOT NULL DEFAULT 0,
                    ""IsActive""         boolean       NOT NULL DEFAULT true,
                    ""CreatedAt""        timestamptz   NOT NULL,
                    ""UpdatedAt""        timestamptz   NOT NULL,
                    ""CreatedBy""        uuid,
                    ""UpdatedBy""        uuid,
                    CONSTRAINT ""PK_vl_content_files"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_vl_content_files_vl_domains_DomainId""
                        FOREIGN KEY (""DomainId"") REFERENCES vl_domains(""Id"") ON DELETE RESTRICT
                );");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_vl_content_files_domain_id ON vl_content_files(""DomainId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_vl_content_files_type      ON vl_content_files(""ContentType"");");

            // 4. Create vl_learning_path_modules
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS vl_learning_path_modules (
                    ""Id""             serial        NOT NULL,
                    ""LearningPathId"" uuid          NOT NULL,
                    ""Title""          varchar(300)  NOT NULL,
                    ""Description""    text,
                    ""SortOrder""      integer       NOT NULL DEFAULT 0,
                    ""IsActive""       boolean       NOT NULL DEFAULT true,
                    CONSTRAINT ""PK_vl_learning_path_modules"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_vl_learning_path_modules_vl_learning_paths_LearningPathId""
                        FOREIGN KEY (""LearningPathId"") REFERENCES vl_learning_paths(""Id"") ON DELETE CASCADE
                );");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_vl_lpm_learning_path_id ON vl_learning_path_modules(""LearningPathId"");");

            // 5. Create vl_learning_path_items
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS vl_learning_path_items (
                    ""Id""             serial        NOT NULL,
                    ""LearningPathId"" uuid          NOT NULL,
                    ""ModuleId""       integer,
                    ""ContentType""    integer       NOT NULL,
                    ""VideoId""        uuid,
                    ""ContentFileId""  uuid,
                    ""TestId""         uuid,
                    ""Title""          varchar(300)  NOT NULL,
                    ""IsPreview""      boolean       NOT NULL DEFAULT false,
                    ""SortOrder""      integer       NOT NULL DEFAULT 0,
                    CONSTRAINT ""PK_vl_learning_path_items"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_vl_learning_path_items_vl_learning_paths_LearningPathId""
                        FOREIGN KEY (""LearningPathId"") REFERENCES vl_learning_paths(""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_vl_learning_path_items_vl_learning_path_modules_ModuleId""
                        FOREIGN KEY (""ModuleId"") REFERENCES vl_learning_path_modules(""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_vl_learning_path_items_vl_videos_VideoId""
                        FOREIGN KEY (""VideoId"") REFERENCES vl_videos(""Id"") ON DELETE SET NULL,
                    CONSTRAINT ""FK_vl_learning_path_items_vl_content_files_ContentFileId""
                        FOREIGN KEY (""ContentFileId"") REFERENCES vl_content_files(""Id"") ON DELETE SET NULL
                );");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_vl_lpi_learning_path_id ON vl_learning_path_items(""LearningPathId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_vl_lpi_module_id        ON vl_learning_path_items(""ModuleId"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS vl_learning_path_items;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS vl_learning_path_modules;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS vl_content_files;");
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name='vl_learning_paths' AND column_name='Type'
                    ) THEN
                        ALTER TABLE vl_learning_paths DROP COLUMN ""Type"";
                    END IF;
                END $$;");
        }
    }
}

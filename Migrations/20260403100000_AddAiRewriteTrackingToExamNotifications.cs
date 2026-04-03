using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridAcademy.Migrations
{
    public partial class AddAiRewriteTrackingToExamNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE exam_notifications
                    ADD COLUMN IF NOT EXISTS is_ai_processed boolean NOT NULL DEFAULT FALSE,
                    ADD COLUMN IF NOT EXISTS ai_processed_at timestamptz;

                CREATE INDEX IF NOT EXISTS ix_exam_notifications_is_ai_processed
                    ON exam_notifications (is_ai_processed);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS ix_exam_notifications_is_ai_processed;

                ALTER TABLE exam_notifications
                    DROP COLUMN IF EXISTS ai_processed_at,
                    DROP COLUMN IF EXISTS is_ai_processed;
                """);
        }
    }
}

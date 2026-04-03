-- Exam content workflow pipeline migration
-- Date: 2026-04-03
-- Purpose:
--   Draft -> AIProcessed -> Approved -> Published
--   enforce published_at for published records

BEGIN;

ALTER TABLE exam_notifications
    ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT NOW(),
    ADD COLUMN IF NOT EXISTS published_at timestamptz;

-- Existing systems may still have status=1 meaning "Published".
-- New enum mapping:
--   0 Draft
--   1 AIProcessed
--   2 Approved
--   3 Published
UPDATE exam_notifications
SET status = 3
WHERE status = 1
  AND (published_at IS NOT NULL OR is_ai_processed = TRUE);

ALTER TABLE exam_notifications
    ALTER COLUMN status SET DEFAULT 0;

CREATE UNIQUE INDEX IF NOT EXISTS ix_exam_notifications_slug ON exam_notifications(slug);
CREATE INDEX IF NOT EXISTS ix_exam_notifications_status ON exam_notifications(status);
CREATE INDEX IF NOT EXISTS ix_exam_notifications_published_at ON exam_notifications(published_at DESC);

ALTER TABLE exam_notifications
    DROP CONSTRAINT IF EXISTS ck_exam_notifications_status,
    DROP CONSTRAINT IF EXISTS ck_exam_notifications_published_at_required;

ALTER TABLE exam_notifications
    ADD CONSTRAINT ck_exam_notifications_status
        CHECK (status IN (0, 1, 2, 3)),
    ADD CONSTRAINT ck_exam_notifications_published_at_required
        CHECK (status <> 3 OR published_at IS NOT NULL);

COMMIT;

-- Exam content module schema (PostgreSQL)
CREATE TABLE IF NOT EXISTS exams (
    id uuid PRIMARY KEY,
    name varchar(200) NOT NULL,
    slug varchar(220) NOT NULL,
    category varchar(100),
    level varchar(100),
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_exams_slug ON exams(slug);
CREATE INDEX IF NOT EXISTS ix_exams_category_level ON exams(category, level);

CREATE TABLE IF NOT EXISTS exam_notifications (
    id uuid PRIMARY KEY,
    exam_id uuid NOT NULL REFERENCES exams(id) ON DELETE CASCADE,
    title varchar(300) NOT NULL,
    slug varchar(320) NOT NULL,
    content_html text NOT NULL,
    summary varchar(500),
    notification_type int NOT NULL,
    important_dates jsonb,
    source_url varchar(500) NOT NULL,
    canonical_url varchar(500),
    meta_title varchar(300),
    meta_description varchar(500),
    published_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    status int NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_exam_notifications_slug ON exam_notifications(slug);
CREATE INDEX IF NOT EXISTS ix_exam_notifications_exam_id ON exam_notifications(exam_id);
CREATE INDEX IF NOT EXISTS ix_exam_notifications_type ON exam_notifications(notification_type);
CREATE INDEX IF NOT EXISTS ix_exam_notifications_published_at ON exam_notifications(published_at DESC);

CREATE TABLE IF NOT EXISTS content_versions (
    id uuid PRIMARY KEY,
    entity_type varchar(80) NOT NULL,
    entity_id uuid NOT NULL,
    content_html text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_content_versions_entity ON content_versions(entity_type, entity_id, created_at DESC);

CREATE TABLE IF NOT EXISTS content_hashes (
    id uuid PRIMARY KEY,
    hash_value varchar(64) NOT NULL,
    source_url varchar(500) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_content_hashes_hash_value ON content_hashes(hash_value);

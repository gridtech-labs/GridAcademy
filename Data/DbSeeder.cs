using GridAcademy.Data.Entities;
using GridAcademy.Data.Entities.CareerGuide;
using GridAcademy.Data.Entities.Content;
using GridAcademy.Data.Entities.Exam;
using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Data;

/// <summary>
/// Seeds default users and master data on first run.
/// Safe to call on every startup — checks before inserting.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        // Apply any pending migrations automatically.
        // On Railway (and other cloud platforms) the database container may not be
        // fully ready when the app starts. Retry with exponential back-off.
        // Fixed 5-second delay between retries (max 20 attempts = ~100s total).
        // Exponential back-off caused Railway's health check to time out when
        // the app blocked startup waiting 128 s between retries.
        // Migration now runs in background (Program.cs) so the HTTP server is
        // already listening; longer retries are fine but 5 s is sufficient.
        const int maxRetries  = 20;
        const int retryDelaySec = 5;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                await EnsureExamContentTablesAsync(db);
                await EnsureManualMigrationColumnsAsync(db);
                break; // success
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                logger.LogWarning(
                    "Database not ready (attempt {Attempt}/{Max}): {Message}. Retrying in {Delay}s…",
                    attempt, maxRetries, ex.Message, retryDelaySec);
                await Task.Delay(TimeSpan.FromSeconds(retryDelaySec));
            }
        }

        // ── Payment tables — run independently so they are applied even when
        //    MigrateAsync fails (e.g. migration conflicts on Railway).
        try { await EnsurePaymentTablesAsync(db); }
        catch (Exception ex) { logger.LogError(ex, "EnsurePaymentTablesAsync failed"); }

        // ── Exam category/sub-category tables and exam_pages FK columns ──────
        try { await EnsureExamCategoryTablesAsync(db); }
        catch (Exception ex) { logger.LogError(ex, "EnsureExamCategoryTablesAsync failed"); }

        // ── Career Guide tables ────────────────────────────────────────────
        try { await EnsureCareerGuideTablesAsync(db); }
        catch (Exception ex) { logger.LogError(ex, "EnsureCareerGuideTablesAsync failed"); }

        // ── AI Generation tables ───────────────────────────────────────────
        try { await EnsureAiGenerationTablesAsync(db); }
        catch (Exception ex) { logger.LogError(ex, "EnsureAiGenerationTablesAsync failed"); }

        // ── Migrate VlDomain → exam_categories, VlVideoCategory → exam_sub_categories
        try { await MigrateVlCategoriesToExamMastersAsync(db); }
        catch (Exception ex) { logger.LogError(ex, "MigrateVlCategoriesToExamMastersAsync failed"); }

        // ── Users ─────────────────────────────────────────────────────────
        const string adminEmail      = "admin@gridacademy.com";
        const string instructorEmail = "instructor@gridacademy.com";

        if (!await db.Users.AnyAsync(u => u.Email == adminEmail))
        {
            db.Users.Add(new User
            {
                FirstName    = "System",
                LastName     = "Admin",
                Email        = adminEmail,
                PasswordHash = PasswordHelper.Hash("Admin@123!"),
                Role         = "SuperAdmin",   // SuperAdmin — platform-wide access
                ClientId     = null,           // no client restriction for SuperAdmin
                IsActive     = true
            });
            logger.LogInformation("Default SuperAdmin seeded → {Email}", adminEmail);
        }

        if (!await db.Users.AnyAsync(u => u.Email == instructorEmail))
        {
            db.Users.Add(new User
            {
                FirstName    = "Demo",
                LastName     = "Instructor",
                Email        = instructorEmail,
                PasswordHash = PasswordHelper.Hash("Instructor@123!"),
                Role         = "Instructor",
                IsActive     = true
            });
            logger.LogInformation("Default instructor seeded → {Email}", instructorEmail);
        }

        await db.SaveChangesAsync();

        // ── System Roles ──────────────────────────────────────────────────
        if (!await db.SystemRoles.AnyAsync())
        {
            db.SystemRoles.AddRange(
                new SystemRole { Name = "Admin",      DisplayName = "Administrator", Description = "Full access — manage users, content, tests, and platform settings.", Color = "danger",   IsSystem = true, IsActive = true, SortOrder = 1, CreatedAt = DateTime.UtcNow },
                new SystemRole { Name = "Instructor", DisplayName = "Instructor",    Description = "Manage content, questions, and tests. Cannot manage users.",          Color = "primary",  IsSystem = true, IsActive = true, SortOrder = 2, CreatedAt = DateTime.UtcNow },
                new SystemRole { Name = "Provider",   DisplayName = "Provider",      Description = "Marketplace provider — can publish test series for sale.",            Color = "purple",   IsSystem = true, IsActive = true, SortOrder = 3, CreatedAt = DateTime.UtcNow },
                new SystemRole { Name = "Student",    DisplayName = "Student",       Description = "Enrolled student — can take tests and access purchased content.",     Color = "success",  IsSystem = true, IsActive = true, SortOrder = 4, CreatedAt = DateTime.UtcNow },
                new SystemRole { Name = "User",       DisplayName = "Standard User", Description = "Default role — basic access to the platform.",                       Color = "secondary",IsSystem = true, IsActive = true, SortOrder = 5, CreatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("System roles seeded (5 roles).");

            // Backfill user_role_maps from existing users.role string
            var users       = await db.Users.Select(u => new { u.Id, u.Role }).ToListAsync();
            var roleIdMap   = await db.SystemRoles.ToDictionaryAsync(r => r.Name, r => r.Id);
            foreach (var u in users)
            {
                if (roleIdMap.TryGetValue(u.Role, out var rid))
                    db.UserRoleMaps.Add(new UserRoleMap { UserId = u.Id, RoleId = rid, AssignedAt = DateTime.UtcNow });
            }
            await db.SaveChangesAsync();
            logger.LogInformation("User role maps backfilled for {Count} users.", users.Count);
        }

        // ── Question Types ────────────────────────────────────────────────
        // IDs MUST match the QuestionType enum values — do NOT change them.
        if (!await db.QuestionTypes.AnyAsync())
        {
            db.QuestionTypes.AddRange(
                new QuestionTypeMaster { Id = QuestionType.MCQ,               Name = "MCQ – Single Correct",    Code = "MCQ",  SortOrder = 1, Description = "One correct option from A–D (JEE Main pattern)" },
                new QuestionTypeMaster { Id = QuestionType.MSQ,               Name = "MSQ – Multiple Select",    Code = "MSQ",  SortOrder = 2, Description = "One or more correct options (JEE Advanced pattern)" },
                new QuestionTypeMaster { Id = QuestionType.NAT,               Name = "NAT – Numerical Answer",   Code = "NAT",  SortOrder = 3, Description = "Integer or decimal answer entered by the student" },
                new QuestionTypeMaster { Id = QuestionType.FillInBlanks,      Name = "FIB – Fill in the Blanks", Code = "FIB",  SortOrder = 4, Description = "One or more blanks in the question text" },
                new QuestionTypeMaster { Id = QuestionType.TrueFalse,         Name = "T/F – True / False",       Code = "TF",   SortOrder = 5, Description = "Student selects True or False" },
                new QuestionTypeMaster { Id = QuestionType.MatchTheFollowing,  Name = "MTF – Match the Following", Code = "MTF", SortOrder = 6, Description = "Match items in List I with List II (1:1 pairing)" },
                new QuestionTypeMaster { Id = QuestionType.AssertionReason,   Name = "ANR – Assertion & Reason",  Code = "ANR",  SortOrder = 7, Description = "Evaluate truth of Assertion A and Reason R separately" },
                new QuestionTypeMaster { Id = QuestionType.PassageBased,      Name = "PBQ – Passage Based",       Code = "PBQ",  SortOrder = 8, Description = "Sub-questions based on a shared reading passage" },
                new QuestionTypeMaster { Id = QuestionType.MatrixMatch,       Name = "MMQ – Matrix Match",        Code = "MMQ",  SortOrder = 9, Description = "Advanced match — each row may map to multiple columns" }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Question types seeded (9 types).");
        }

        // ── Subjects ──────────────────────────────────────────────────────
        if (!await db.Subjects.AnyAsync())
        {
            db.Subjects.AddRange(
                new Subject { Name = "Physics",     SortOrder = 1 },
                new Subject { Name = "Chemistry",   SortOrder = 2 },
                new Subject { Name = "Mathematics", SortOrder = 3 },
                new Subject { Name = "Biology",     SortOrder = 4 }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Subjects seeded.");
        }

        // ── Topics (sample per subject) ───────────────────────────────────
        if (!await db.Topics.AnyAsync())
        {
            var subjects = await db.Subjects.ToDictionaryAsync(s => s.Name, s => s.Id);
            db.Topics.AddRange(
                // Physics
                new Topic { Name = "Mechanics",          SubjectId = subjects["Physics"],     SortOrder = 1 },
                new Topic { Name = "Electrostatics",     SubjectId = subjects["Physics"],     SortOrder = 2 },
                new Topic { Name = "Optics",             SubjectId = subjects["Physics"],     SortOrder = 3 },
                // Chemistry
                new Topic { Name = "Organic Chemistry",  SubjectId = subjects["Chemistry"],   SortOrder = 1 },
                new Topic { Name = "Inorganic Chemistry",SubjectId = subjects["Chemistry"],   SortOrder = 2 },
                new Topic { Name = "Physical Chemistry", SubjectId = subjects["Chemistry"],   SortOrder = 3 },
                // Mathematics
                new Topic { Name = "Calculus",           SubjectId = subjects["Mathematics"], SortOrder = 1 },
                new Topic { Name = "Algebra",            SubjectId = subjects["Mathematics"], SortOrder = 2 },
                new Topic { Name = "Coordinate Geometry",SubjectId = subjects["Mathematics"], SortOrder = 3 },
                // Biology
                new Topic { Name = "Cell Biology",       SubjectId = subjects["Biology"],     SortOrder = 1 },
                new Topic { Name = "Genetics",           SubjectId = subjects["Biology"],     SortOrder = 2 },
                new Topic { Name = "Ecology",            SubjectId = subjects["Biology"],     SortOrder = 3 }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Topics seeded.");
        }

        // ── Difficulty Levels ─────────────────────────────────────────────
        if (!await db.DifficultyLevels.AnyAsync())
        {
            db.DifficultyLevels.AddRange(
                new DifficultyLevel { Name = "Easy",   SortOrder = 1 },
                new DifficultyLevel { Name = "Medium", SortOrder = 2 },
                new DifficultyLevel { Name = "Hard",   SortOrder = 3 }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Difficulty levels seeded.");
        }

        // ── Complexity Levels ─────────────────────────────────────────────
        if (!await db.ComplexityLevels.AnyAsync())
        {
            db.ComplexityLevels.AddRange(
                new ComplexityLevel { Name = "Low",    SortOrder = 1 },
                new ComplexityLevel { Name = "Medium", SortOrder = 2 },
                new ComplexityLevel { Name = "High",   SortOrder = 3 }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Complexity levels seeded.");
        }

        // ── Exam Types ────────────────────────────────────────────────────
        if (!await db.ExamTypes.AnyAsync())
        {
            db.ExamTypes.AddRange(
                new ExamType { Name = "JEE Main",     SortOrder = 1 },
                new ExamType { Name = "JEE Advanced", SortOrder = 2 },
                new ExamType { Name = "NEET",         SortOrder = 3 },
                new ExamType { Name = "Board",        SortOrder = 4 }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Exam types seeded.");
        }

        // ── Marks ─────────────────────────────────────────────────────────
        if (!await db.MarksMaster.AnyAsync())
        {
            db.MarksMaster.AddRange(
                new MarksMaster { Name = "1 Mark",  Value = 1,  SortOrder = 1 },
                new MarksMaster { Name = "2 Marks", Value = 2,  SortOrder = 2 },
                new MarksMaster { Name = "3 Marks", Value = 3,  SortOrder = 3 },
                new MarksMaster { Name = "4 Marks", Value = 4,  SortOrder = 4 }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Marks seeded.");
        }

        // ── Negative Marks ────────────────────────────────────────────────
        if (!await db.NegativeMarksMaster.AnyAsync())
        {
            db.NegativeMarksMaster.AddRange(
                new NegativeMarksMaster { Name = "No Negative",  Value = 0,      SortOrder = 1 },
                new NegativeMarksMaster { Name = "-0.25 Marks",  Value = -0.25m, SortOrder = 2 },
                new NegativeMarksMaster { Name = "-1 Mark",      Value = -1,     SortOrder = 3 },
                new NegativeMarksMaster { Name = "-2 Marks",     Value = -2,     SortOrder = 4 }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Negative marks seeded.");
        }

        // ── RRB ALP master data ───────────────────────────────────────────────
        // Idempotent — checks by name before inserting. Safe to re-run.

        // New subjects (General Science, Current Affairs, Reasoning, Technical)
        var existingSubjectNames = await db.Subjects.Select(s => s.Name).ToListAsync();
        var rrbSubjects = new (string Name, int Order)[]
        {
            ("General Science", 5), ("Current Affairs", 6),
            ("Reasoning", 7),       ("Technical", 8)
        };
        bool subjectsAdded = false;
        foreach (var (name, order) in rrbSubjects)
        {
            if (!existingSubjectNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            { db.Subjects.Add(new Subject { Name = name, SortOrder = order }); subjectsAdded = true; }
        }
        if (subjectsAdded) { await db.SaveChangesAsync(); logger.LogInformation("RRB ALP subjects seeded."); }

        // New topics (keyed by subject name → topic)
        var allSubjectMap = await db.Subjects.ToDictionaryAsync(s => s.Name, s => s.Id);
        var existingTopicPairs = await db.Topics.Select(t => new { t.Name, t.SubjectId }).ToListAsync();
        var rrbTopics = new (string Subject, string Name, int Order)[]
        {
            ("General Science", "Biology",         4), ("General Science", "Chemistry",       5),
            ("General Science", "Physics",          6),
            ("Current Affairs", "Awards",           1), ("Current Affairs", "International",   2),
            ("Current Affairs", "National",         3), ("Current Affairs", "Sports",          4),
            ("Reasoning",       "Analogy",          1), ("Reasoning",       "Coding-Decoding", 2),
            ("Reasoning",       "Puzzle",           3), ("Reasoning",       "Series",          4),
            ("Technical",       "Electrical",       1), ("Technical",       "Electronics",     2),
            ("Technical",       "Mechanical",       3),
        };
        bool topicsAdded = false;
        foreach (var (subjectName, topicName, order) in rrbTopics)
        {
            if (!allSubjectMap.TryGetValue(subjectName, out var sid)) continue;
            if (existingTopicPairs.Any(t => t.Name == topicName && t.SubjectId == sid)) continue;
            db.Topics.Add(new Topic { Name = topicName, SubjectId = sid, SortOrder = order });
            topicsAdded = true;
        }
        if (topicsAdded) { await db.SaveChangesAsync(); logger.LogInformation("RRB ALP topics seeded."); }

        // RRB ALP exam type
        if (!await db.ExamTypes.AnyAsync(e => e.Name == "RRB ALP"))
        {
            var maxOrder = await db.ExamTypes.MaxAsync(e => (int?)e.SortOrder) ?? 0;
            db.ExamTypes.Add(new ExamType { Name = "RRB ALP", SortOrder = maxOrder + 1 });
            await db.SaveChangesAsync();
            logger.LogInformation("RRB ALP exam type seeded.");
        }

        // -0.33 mark (RRB 1/3 negative marking)
        if (!await db.NegativeMarksMaster.AnyAsync(n => n.Name == "-0.33 Mark"))
        {
            db.NegativeMarksMaster.Add(new NegativeMarksMaster { Name = "-0.33 Mark", Value = -0.33m, SortOrder = 2 });
            await db.SaveChangesAsync();
            logger.LogInformation("-0.33 Mark negative marking seeded.");
        }

        // ── Exam Levels ───────────────────────────────────────────────────
        if (!await db.ExamLevels.AnyAsync())
        {
            db.ExamLevels.AddRange(
                new ExamLevel { Name = "All India Level", SortOrder = 1 },
                new ExamLevel { Name = "State Level",     SortOrder = 2 },
                new ExamLevel { Name = "University Exam", SortOrder = 3 },
                new ExamLevel { Name = "School Exam",     SortOrder = 4 }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Exam levels seeded.");
        }

        // ── Default VL Domain (needed for Learning Path builder) ──────────
        if (!await db.VlDomains.AnyAsync())
        {
            db.VlDomains.Add(new VlDomain
            {
                Name = "General", Description = "Default domain", IsActive = true, SortOrder = 0
            });
            await db.SaveChangesAsync();
            logger.LogInformation("Default VL domain seeded.");
        }
    }

    private static async Task EnsureExamContentTablesAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS content_hashes (
                id uuid PRIMARY KEY,
                hash_value character varying(64) NOT NULL,
                source_url character varying(500) NOT NULL,
                created_at timestamptz NOT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS ix_content_hashes_hash_value
            ON content_hashes (hash_value);
            """);
    }

    /// <summary>
    /// Idempotent raw-SQL method that applies schema changes which were introduced
    /// outside of proper EF migrations (local psql patches).  Safe to re-run on
    /// every startup — every statement uses IF NOT EXISTS / DO-block guards.
    /// </summary>
    private static async Task EnsureManualMigrationColumnsAsync(AppDbContext db)
    {
        // ── 1. tests.question_mode ────────────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE tests
                ADD COLUMN IF NOT EXISTS question_mode integer NOT NULL DEFAULT 0;
            """);

        // ── 2. test_sections.subject_id → make nullable ───────────────────────
        // PostgreSQL has no "ALTER COLUMN … DROP NOT NULL IF EXISTS" shorthand,
        // so we use a DO block that checks the column's nullable flag first.
        await db.Database.ExecuteSqlRawAsync(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE  table_name  = 'test_sections'
                    AND    column_name = 'subject_id'
                    AND    is_nullable = 'NO'
                ) THEN
                    ALTER TABLE test_sections ALTER COLUMN subject_id DROP NOT NULL;
                END IF;
            END;
            $$;
            """);

        // ── 3. test_questions.section_id ──────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE test_questions
                ADD COLUMN IF NOT EXISTS section_id integer;
            """);

        // ── 4. FK + index for test_questions.section_id ───────────────────────
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS ix_test_questions_section_id
                ON test_questions (section_id);
            """);

        await db.Database.ExecuteSqlRawAsync(
            """
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE  constraint_type = 'FOREIGN KEY'
                    AND    table_name      = 'test_questions'
                    AND    constraint_name = 'fk_test_questions_section_id'
                ) THEN
                    ALTER TABLE test_questions
                        ADD CONSTRAINT fk_test_questions_section_id
                        FOREIGN KEY (section_id) REFERENCES test_sections(id)
                        ON DELETE SET NULL;
                END IF;
            END;
            $$;
            """);

        // ── 5. system_roles table ─────────────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS system_roles (
                id           serial      PRIMARY KEY,
                name         varchar(50) NOT NULL,
                display_name varchar(100) NOT NULL,
                description  text,
                color        varchar(30),
                is_system    boolean     NOT NULL DEFAULT false,
                is_active    boolean     NOT NULL DEFAULT true,
                sort_order   integer     NOT NULL DEFAULT 0,
                created_at   timestamptz NOT NULL DEFAULT now()
            );
            """);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS ix_system_roles_name
                ON system_roles (name);
            """);

        // ── 6. user_role_maps table ───────────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS user_role_maps (
                id          serial      PRIMARY KEY,
                user_id     uuid        NOT NULL,
                role_id     integer     NOT NULL,
                assigned_at timestamptz NOT NULL DEFAULT now(),
                assigned_by uuid,
                CONSTRAINT fk_urm_user FOREIGN KEY (user_id) REFERENCES users(id)  ON DELETE CASCADE,
                CONSTRAINT fk_urm_role FOREIGN KEY (role_id) REFERENCES system_roles(id) ON DELETE CASCADE
            );
            """);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS ix_user_role_maps_user_role
                ON user_role_maps (user_id, role_id);
            """);

        // ── 7. Seed default system roles (if table is empty) ─────────────────
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO system_roles (name, display_name, description, color, is_system, is_active, sort_order, created_at)
            SELECT name, display_name, description, color, is_system, is_active, sort_order, now()
            FROM (VALUES
                ('Admin',      'Administrator', 'Full access — manage users, content, tests, and platform settings.', 'danger',    true,  true, 1),
                ('Instructor', 'Instructor',    'Manage content, questions, and tests. Cannot manage users.',          'primary',   true,  true, 2),
                ('Provider',   'Provider',      'Marketplace provider — can publish test series for sale.',            'purple',    true,  true, 3),
                ('Student',    'Student',       'Enrolled student — can take tests and access purchased content.',     'success',   true,  true, 4),
                ('User',       'Standard User', 'Default role — basic access to the platform.',                       'secondary', true,  true, 5)
            ) AS t(name, display_name, description, color, is_system, is_active, sort_order)
            WHERE NOT EXISTS (SELECT 1 FROM system_roles LIMIT 1);
            """);

        // ── 8. Backfill user_role_maps from users.role (idempotent) ──────────
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO user_role_maps (user_id, role_id, assigned_at)
            SELECT u.id, sr.id, now()
            FROM   users u
            JOIN   system_roles sr ON sr.name = u.role
            WHERE  NOT EXISTS (
                SELECT 1 FROM user_role_maps m
                WHERE  m.user_id = u.id
            );
            """);

        // ── 9. instructions_acknowledged column on test_attempts ─────────────
        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE test_attempts
                ADD COLUMN IF NOT EXISTS instructions_acknowledged boolean NOT NULL DEFAULT false;
            """);

        // ── 10. clients table ─────────────────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS clients (
                id          integer  GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                name        varchar(200) NOT NULL,
                slug        varchar(220) NOT NULL,
                description text,
                logo_url    varchar(500),
                is_active   boolean  NOT NULL DEFAULT true,
                created_at  timestamptz NOT NULL DEFAULT now(),
                updated_at  timestamptz
            );
            """);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS ix_clients_slug ON clients (slug);
            """);

        // ── 11. users.client_id column ────────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE users ADD COLUMN IF NOT EXISTS client_id integer;
            """);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS ix_users_client_id ON users (client_id);
            """);

        // ── 12. FK: users.client_id → clients.id ─────────────────────────────
        await db.Database.ExecuteSqlRawAsync(
            """
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE  constraint_type = 'FOREIGN KEY'
                    AND    table_name      = 'users'
                    AND    constraint_name = 'fk_users_client_id'
                ) THEN
                    ALTER TABLE users
                        ADD CONSTRAINT fk_users_client_id
                        FOREIGN KEY (client_id) REFERENCES clients(id)
                        ON DELETE SET NULL;
                END IF;
            END;
            $$;
            """);

        // ── 13. Seed default "GridAcademy" client ─────────────────────────────
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO clients (name, slug, description, is_active, created_at)
            SELECT 'GridAcademy', 'gridacademy', 'Default platform client.', true, now()
            WHERE  NOT EXISTS (SELECT 1 FROM clients WHERE slug = 'gridacademy');
            """);

        // ── 14. Backfill all existing users to the GridAcademy client ─────────
        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE users
               SET client_id = (SELECT id FROM clients WHERE slug = 'gridacademy' LIMIT 1)
             WHERE client_id IS NULL;
            """);

        // ── 15. Add SuperAdmin to system_roles ───────────────────────────────
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO system_roles
                   (name, display_name, description, color, is_system, is_active, sort_order, created_at)
            SELECT 'SuperAdmin', 'Super Administrator',
                   'Platform-wide super admin — full access across all clients.',
                   'dark', true, true, 0, now()
            WHERE  NOT EXISTS (SELECT 1 FROM system_roles WHERE name = 'SuperAdmin');
            """);

        // ── 16-1. groups table ───────────────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS groups (
                id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                name        varchar(200) NOT NULL,
                description text,
                client_id   integer,
                is_active   boolean     NOT NULL DEFAULT true,
                created_at  timestamptz NOT NULL DEFAULT now(),
                updated_at  timestamptz,
                created_by  uuid
            );
            """);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS ix_groups_client_id ON groups (client_id);
            """);

        await db.Database.ExecuteSqlRawAsync(
            """
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE  constraint_type = 'FOREIGN KEY'
                    AND    table_name      = 'groups'
                    AND    constraint_name = 'fk_groups_client_id'
                ) THEN
                    ALTER TABLE groups
                        ADD CONSTRAINT fk_groups_client_id
                        FOREIGN KEY (client_id) REFERENCES clients(id)
                        ON DELETE SET NULL;
                END IF;
            END;
            $$;
            """);

        // ── 16-2. user_groups junction table ─────────────────────────────────
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS user_groups (
                id        integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                group_id  integer     NOT NULL,
                user_id   uuid        NOT NULL,
                added_at  timestamptz NOT NULL DEFAULT now(),
                added_by  uuid
            );
            """);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS ix_user_groups_group_user
                ON user_groups (group_id, user_id);
            """);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS ix_user_groups_user_id
                ON user_groups (user_id);
            """);

        await db.Database.ExecuteSqlRawAsync(
            """
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE  constraint_type = 'FOREIGN KEY'
                    AND    table_name      = 'user_groups'
                    AND    constraint_name = 'fk_user_groups_group_id'
                ) THEN
                    ALTER TABLE user_groups
                        ADD CONSTRAINT fk_user_groups_group_id
                        FOREIGN KEY (group_id) REFERENCES groups(id)
                        ON DELETE CASCADE;
                END IF;
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE  constraint_type = 'FOREIGN KEY'
                    AND    table_name      = 'user_groups'
                    AND    constraint_name = 'fk_user_groups_user_id'
                ) THEN
                    ALTER TABLE user_groups
                        ADD CONSTRAINT fk_user_groups_user_id
                        FOREIGN KEY (user_id) REFERENCES users(id)
                        ON DELETE CASCADE;
                END IF;
            END;
            $$;
            """);

        // ── 17. group_id column on test_assignments ───────────────────────────
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE test_assignments
                ADD COLUMN IF NOT EXISTS group_id INT;
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS ix_test_assignments_group_id
                ON test_assignments (group_id)
                WHERE group_id IS NOT NULL;
            """);
        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                     WHERE table_schema    = 'public'
                       AND table_name      = 'test_assignments'
                       AND constraint_name = 'fk_test_assignments_group_id'
                ) THEN
                    ALTER TABLE test_assignments
                        ADD CONSTRAINT fk_test_assignments_group_id
                        FOREIGN KEY (group_id) REFERENCES groups(id)
                        ON DELETE SET NULL;
                END IF;
            END;
            $$;
            """);

        // ── 16. Promote admin@gridacademy.com to SuperAdmin ──────────────────
        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE users
               SET role = 'SuperAdmin', client_id = NULL
             WHERE email = 'admin@gridacademy.com'
               AND role  = 'Admin';
            """);

        // Keep user_role_maps in sync for the promoted admin
        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE user_role_maps urm
               SET role_id = (SELECT id FROM system_roles WHERE name = 'SuperAdmin' LIMIT 1)
              FROM users u
             WHERE urm.user_id = u.id
               AND u.email     = 'admin@gridacademy.com'
               AND EXISTS (SELECT 1 FROM system_roles WHERE name = 'SuperAdmin');
            """);

    }

    private static async Task EnsureExamCategoryTablesAsync(AppDbContext db)
    {
        // exam_categories
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS exam_categories (
                id        integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                name      character varying(150) NOT NULL,
                is_active boolean NOT NULL DEFAULT true,
                sort_order integer NOT NULL DEFAULT 0
            );
            """);

        // exam_sub_categories
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS exam_sub_categories (
                id               integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                exam_category_id integer NOT NULL,
                name             character varying(150) NOT NULL,
                is_active        boolean NOT NULL DEFAULT true,
                sort_order       integer NOT NULL DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS ix_exam_sub_categories_category_id
                ON exam_sub_categories (exam_category_id);
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE constraint_name = 'FK_exam_sub_categories_exam_categories_exam_category_id'
                ) THEN
                    ALTER TABLE exam_sub_categories
                        ADD CONSTRAINT "FK_exam_sub_categories_exam_categories_exam_category_id"
                        FOREIGN KEY (exam_category_id) REFERENCES exam_categories(id)
                        ON DELETE CASCADE;
                END IF;
            END; $$;
            """);

        // ExamCategoryId + ExamSubCategoryId columns on exam_pages
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE exam_pages ADD COLUMN IF NOT EXISTS "ExamCategoryId" integer;
            ALTER TABLE exam_pages ADD COLUMN IF NOT EXISTS "ExamSubCategoryId" integer;
            CREATE INDEX IF NOT EXISTS ix_exam_pages_exam_category_id ON exam_pages ("ExamCategoryId");
            CREATE INDEX IF NOT EXISTS ix_exam_pages_exam_sub_category_id ON exam_pages ("ExamSubCategoryId");
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE constraint_name = 'FK_exam_pages_exam_categories_ExamCategoryId'
                ) THEN
                    ALTER TABLE exam_pages
                        ADD CONSTRAINT "FK_exam_pages_exam_categories_ExamCategoryId"
                        FOREIGN KEY ("ExamCategoryId") REFERENCES exam_categories(id)
                        ON DELETE SET NULL;
                END IF;
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE constraint_name = 'FK_exam_pages_exam_sub_categories_ExamSubCategoryId'
                ) THEN
                    ALTER TABLE exam_pages
                        ADD CONSTRAINT "FK_exam_pages_exam_sub_categories_ExamSubCategoryId"
                        FOREIGN KEY ("ExamSubCategoryId") REFERENCES exam_sub_categories(id)
                        ON DELETE SET NULL;
                END IF;
            END; $$;
            """);

        // Drop exam_type_id from tests if still present (safe to run multiple times)
        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'tests' AND column_name = 'exam_type_id'
                ) THEN
                    ALTER TABLE tests DROP CONSTRAINT IF EXISTS "FK_tests_exam_types_exam_type_id";
                    DROP INDEX IF EXISTS ix_tests_exam_type_id;
                    ALTER TABLE tests DROP COLUMN exam_type_id;
                END IF;
            END; $$;
            """);

        // Drop Category string column from exam_pages if still present
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE exam_pages DROP COLUMN IF EXISTS "Category";
            """);
    }

    // One-time migration: copy VlDomain → exam_categories, VlVideoCategory → exam_sub_categories.
    // Runs only when exam_categories is empty to avoid duplicates on subsequent startups.
    private static async Task MigrateVlCategoriesToExamMastersAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                -- Only run if exam_categories is still empty
                IF NOT EXISTS (SELECT 1 FROM exam_categories LIMIT 1) THEN

                    -- 1. Copy vl_domains → exam_categories (preserve sort_order and is_active)
                    INSERT INTO exam_categories (name, is_active, sort_order)
                    SELECT name, is_active, sort_order
                    FROM   vl_domains
                    ORDER  BY sort_order, name;

                    -- 2. Copy vl_video_categories → exam_sub_categories,
                    --    mapping old domain_id → new exam_category_id by name match
                    INSERT INTO exam_sub_categories (exam_category_id, name, is_active, sort_order)
                    SELECT ec.id, vc.name, vc.is_active, vc.sort_order
                    FROM   vl_video_categories vc
                    JOIN   vl_domains           vd ON vd.id = vc.domain_id
                    JOIN   exam_categories      ec ON ec.name = vd.name
                    ORDER  BY ec.id, vc.sort_order, vc.name;

                END IF;
            END;
            $$;
            """);
    }

    // ── Payment tables — separate method called independently of MigrateAsync ──
    // NO foreign key constraints in raw SQL — FK resolution differs by deploy state.
    // EF Core enforces referential integrity at the ORM level.
    // Each table is wrapped in its own try-catch so one failure never blocks the rest.
    private static async Task EnsurePaymentTablesAsync(AppDbContext db)
    {
        var sqls = new[]
        {
            """
            CREATE TABLE IF NOT EXISTS "ExamOffers" (
                "Id"                serial       PRIMARY KEY,
                "Code"              varchar(50)  NOT NULL DEFAULT '',
                "Title"             varchar(120) NOT NULL DEFAULT '',
                "Description"       varchar(500),
                "OfferType"         integer      NOT NULL DEFAULT 0,
                "Value"             numeric      NOT NULL DEFAULT 0,
                "MinOrderAmount"    numeric      NOT NULL DEFAULT 0,
                "MaxDiscountAmount" numeric,
                "ExamPageId"        uuid,
                "MaxUses"           integer,
                "UsedCount"         integer      NOT NULL DEFAULT 0,
                "IsActive"          boolean      NOT NULL DEFAULT true,
                "ValidFrom"         timestamptz,
                "ValidTo"           timestamptz,
                "CreatedAt"         timestamptz  NOT NULL DEFAULT now(),
                "UpdatedAt"         timestamptz  NOT NULL DEFAULT now()
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS "ExamOrders" (
                "Id"                uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
                "StudentId"         uuid        NOT NULL,
                "ExamPageId"        uuid        NOT NULL,
                "OriginalAmount"    numeric     NOT NULL DEFAULT 0,
                "DiscountAmount"    numeric     NOT NULL DEFAULT 0,
                "FinalAmount"       numeric     NOT NULL DEFAULT 0,
                "GstAmount"         numeric     NOT NULL DEFAULT 0,
                "GrandTotal"        numeric     NOT NULL DEFAULT 0,
                "OfferId"           integer,
                "OfferCode"         varchar(50),
                "RazorpayOrderId"   varchar(100),
                "RazorpayPaymentId" varchar(100),
                "RazorpaySignature" varchar(256),
                "Status"            integer     NOT NULL DEFAULT 0,
                "BookingRef"        varchar(30) NOT NULL DEFAULT '',
                "FailureReason"     varchar(500),
                "Notes"             varchar(500),
                "CreatedAt"         timestamptz NOT NULL DEFAULT now(),
                "UpdatedAt"         timestamptz NOT NULL DEFAULT now(),
                "PaidAt"            timestamptz
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS "ExamAccesses" (
                "Id"         uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
                "StudentId"  uuid        NOT NULL,
                "ExamPageId" uuid        NOT NULL,
                "OrderId"    uuid        NOT NULL,
                "GrantedAt"  timestamptz NOT NULL DEFAULT now(),
                "ExpiresAt"  timestamptz,
                "IsActive"   boolean     NOT NULL DEFAULT true
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS "ExamOrderTransactions" (
                "Id"                uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
                "ExamOrderId"       uuid        NOT NULL,
                "Event"             varchar(80) NOT NULL DEFAULT '',
                "RazorpayPaymentId" varchar(100),
                "RazorpayOrderId"   varchar(100),
                "Amount"            numeric,
                "RawPayload"        text,
                "CreatedAt"         timestamptz NOT NULL DEFAULT now()
            )
            """,
            // Indexes — safe to run every time
            @"CREATE INDEX IF NOT EXISTS ""IX_ExamOrders_StudentId""              ON ""ExamOrders""(""StudentId"")",
            @"CREATE INDEX IF NOT EXISTS ""IX_ExamOrders_ExamPageId""             ON ""ExamOrders""(""ExamPageId"")",
            @"CREATE INDEX IF NOT EXISTS ""IX_ExamAccesses_StudentId""            ON ""ExamAccesses""(""StudentId"")",
            @"CREATE INDEX IF NOT EXISTS ""IX_ExamAccesses_ExamPageId""           ON ""ExamAccesses""(""ExamPageId"")",
            @"CREATE INDEX IF NOT EXISTS ""IX_ExamOrderTransactions_ExamOrderId"" ON ""ExamOrderTransactions""(""ExamOrderId"")",
        };

        foreach (var sql in sqls)
        {
            try   { await db.Database.ExecuteSqlRawAsync(sql); }
            catch { /* table/index already exists or minor schema mismatch — continue */ }
        }
    }

    private static async Task EnsureCareerGuideTablesAsync(AppDbContext db)
    {
        var sqls = new[]
        {
            @"CREATE TABLE IF NOT EXISTS career_quiz_questions (
                id           SERIAL PRIMARY KEY,
                question_text TEXT NOT NULL,
                sort_order   INT  NOT NULL DEFAULT 0,
                is_active    BOOL NOT NULL DEFAULT TRUE,
                created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
            )",
            @"CREATE TABLE IF NOT EXISTS career_quiz_options (
                id              SERIAL PRIMARY KEY,
                question_id     INT  NOT NULL REFERENCES career_quiz_questions(id) ON DELETE CASCADE,
                option_text     TEXT NOT NULL,
                career_category VARCHAR(50) NOT NULL DEFAULT '',
                sort_order      INT  NOT NULL DEFAULT 0
            )",
            @"CREATE INDEX IF NOT EXISTS ix_career_quiz_options_question_id ON career_quiz_options(question_id)",
        };

        foreach (var sql in sqls)
        {
            try   { await db.Database.ExecuteSqlRawAsync(sql); }
            catch { /* already exists — continue */ }
        }

        // ── Seed 5 default quiz questions (only if table is empty) ────────────
        await SeedCareerQuizQuestionsAsync(db);
    }

    // ── AI Generation tables ─────────────────────────────────────────────────
    // Each statement is individually guarded so one failure never blocks the rest.
    private static async Task EnsureAiGenerationTablesAsync(AppDbContext db)
    {
        // ── 18. Enable pgvector extension ─────────────────────────────────────
        // Wrapped in a try/catch — on Railway the superuser role is needed.
        // If it fails the rest of the tables still work; embeddings just stay off.
        try
        {
            await db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS vector;");
        }
        catch { /* pgvector not available — embeddings feature disabled */ }

        // ── 19. is_ai_generated column on questions ───────────────────────────
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE questions
                ADD COLUMN IF NOT EXISTS is_ai_generated boolean NOT NULL DEFAULT false;
            """);

        // ── 20. question_translations ─────────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS question_translations (
                id                  integer     GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                source_question_id  uuid        NOT NULL,
                language            varchar(10) NOT NULL DEFAULT 'hi',
                text                text        NOT NULL DEFAULT '',
                solution            text,
                options_json        text        NOT NULL DEFAULT '[]',
                created_at          timestamptz NOT NULL DEFAULT now(),
                updated_at          timestamptz NOT NULL DEFAULT now(),
                created_by          uuid
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS ix_question_translations_source_lang
                ON question_translations (source_question_id, language);
            """);

        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE  constraint_type = 'FOREIGN KEY'
                    AND    table_name      = 'question_translations'
                    AND    constraint_name = 'fk_question_translations_source_question'
                ) THEN
                    ALTER TABLE question_translations
                        ADD CONSTRAINT fk_question_translations_source_question
                        FOREIGN KEY (source_question_id) REFERENCES questions(id)
                        ON DELETE CASCADE;
                END IF;
            END; $$;
            """);

        // ── 21. ai_exam_configs ───────────────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ai_exam_configs (
                id              integer     GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                exam_page_id    uuid        NOT NULL,
                is_ai_enabled   boolean     NOT NULL DEFAULT false,
                notes           text,
                created_at      timestamptz NOT NULL DEFAULT now(),
                updated_at      timestamptz NOT NULL DEFAULT now()
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS ix_ai_exam_configs_exam_page_id
                ON ai_exam_configs (exam_page_id);
            """);

        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE  constraint_type = 'FOREIGN KEY'
                    AND    table_name      = 'ai_exam_configs'
                    AND    constraint_name = 'fk_ai_exam_configs_exam_page_id'
                ) THEN
                    ALTER TABLE ai_exam_configs
                        ADD CONSTRAINT fk_ai_exam_configs_exam_page_id
                        FOREIGN KEY (exam_page_id) REFERENCES exam_pages(id)
                        ON DELETE CASCADE;
                END IF;
            END; $$;
            """);

        // ── 22. ai_exam_sections ──────────────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ai_exam_sections (
                id                  integer      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                ai_exam_config_id   integer      NOT NULL,
                section_name        varchar(200) NOT NULL DEFAULT '',
                subject_id          integer,
                weightage_percent   numeric(5,2),
                number_of_questions integer      NOT NULL DEFAULT 25,
                sort_order          integer      NOT NULL DEFAULT 0,
                marks_id            integer,
                negative_marks_id   integer,
                created_at          timestamptz  NOT NULL DEFAULT now()
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS ix_ai_exam_sections_config_id
                ON ai_exam_sections (ai_exam_config_id);
            """);

        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE  constraint_type = 'FOREIGN KEY'
                    AND    table_name      = 'ai_exam_sections'
                    AND    constraint_name = 'fk_ai_exam_sections_config_id'
                ) THEN
                    ALTER TABLE ai_exam_sections
                        ADD CONSTRAINT fk_ai_exam_sections_config_id
                        FOREIGN KEY (ai_exam_config_id) REFERENCES ai_exam_configs(id)
                        ON DELETE CASCADE;
                END IF;
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE  constraint_type = 'FOREIGN KEY'
                    AND    table_name      = 'ai_exam_sections'
                    AND    constraint_name = 'fk_ai_exam_sections_subject_id'
                ) THEN
                    ALTER TABLE ai_exam_sections
                        ADD CONSTRAINT fk_ai_exam_sections_subject_id
                        FOREIGN KEY (subject_id) REFERENCES subjects(id)
                        ON DELETE SET NULL;
                END IF;
            END; $$;
            """);

        // ── 23. ai_exam_topics ────────────────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ai_exam_topics (
                id                       integer      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                ai_exam_section_id       integer      NOT NULL,
                topic_id                 integer,
                topic_name               varchar(200) NOT NULL DEFAULT '',
                syllabus_text            text,
                reference_questions_json jsonb,
                sort_order               integer      NOT NULL DEFAULT 0,
                created_at               timestamptz  NOT NULL DEFAULT now()
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS ix_ai_exam_topics_section_id
                ON ai_exam_topics (ai_exam_section_id);
            """);

        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE  constraint_type = 'FOREIGN KEY'
                    AND    table_name      = 'ai_exam_topics'
                    AND    constraint_name = 'fk_ai_exam_topics_section_id'
                ) THEN
                    ALTER TABLE ai_exam_topics
                        ADD CONSTRAINT fk_ai_exam_topics_section_id
                        FOREIGN KEY (ai_exam_section_id) REFERENCES ai_exam_sections(id)
                        ON DELETE CASCADE;
                END IF;
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE  constraint_type = 'FOREIGN KEY'
                    AND    table_name      = 'ai_exam_topics'
                    AND    constraint_name = 'fk_ai_exam_topics_topic_id'
                ) THEN
                    ALTER TABLE ai_exam_topics
                        ADD CONSTRAINT fk_ai_exam_topics_topic_id
                        FOREIGN KEY (topic_id) REFERENCES topics(id)
                        ON DELETE SET NULL;
                END IF;
            END; $$;
            """);

        // ── 24. generation_prompt_templates ───────────────────────────────────
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS generation_prompt_templates (
                id                  integer     GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                ai_exam_config_id   integer     NOT NULL,
                ai_exam_section_id  integer,
                version             integer     NOT NULL DEFAULT 1,
                is_active           boolean     NOT NULL DEFAULT true,
                prompt_text         text        NOT NULL DEFAULT '',
                created_at          timestamptz NOT NULL DEFAULT now(),
                created_by          uuid
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS ix_generation_prompt_templates_config_id
                ON generation_prompt_templates (ai_exam_config_id);
            """);

        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE  constraint_type = 'FOREIGN KEY'
                    AND    table_name      = 'generation_prompt_templates'
                    AND    constraint_name = 'fk_generation_prompt_templates_config_id'
                ) THEN
                    ALTER TABLE generation_prompt_templates
                        ADD CONSTRAINT fk_generation_prompt_templates_config_id
                        FOREIGN KEY (ai_exam_config_id) REFERENCES ai_exam_configs(id)
                        ON DELETE CASCADE;
                END IF;
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE  constraint_type = 'FOREIGN KEY'
                    AND    table_name      = 'generation_prompt_templates'
                    AND    constraint_name = 'fk_generation_prompt_templates_section_id'
                ) THEN
                    ALTER TABLE generation_prompt_templates
                        ADD CONSTRAINT fk_generation_prompt_templates_section_id
                        FOREIGN KEY (ai_exam_section_id) REFERENCES ai_exam_sections(id)
                        ON DELETE SET NULL;
                END IF;
            END; $$;
            """);

        // ── 25. generation_jobs ───────────────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS generation_jobs (
                id                       integer      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                exam_page_id             uuid         NOT NULL,
                ai_exam_section_id       integer,
                ai_exam_topic_id         integer,
                difficulty               varchar(20)  NOT NULL DEFAULT 'medium',
                language                 varchar(10)  NOT NULL DEFAULT 'en',
                count                    integer      NOT NULL DEFAULT 10,
                notes                    text,
                status                   integer      NOT NULL DEFAULT 1,
                generated                integer      NOT NULL DEFAULT 0,
                auto_flagged             integer      NOT NULL DEFAULT 0,
                auto_rejected            integer      NOT NULL DEFAULT 0,
                error_message            text,
                hangfire_job_id          varchar(100),
                prompt_template_version  integer,
                created_at               timestamptz  NOT NULL DEFAULT now(),
                completed_at             timestamptz,
                created_by               uuid
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS ix_generation_jobs_exam_page_id ON generation_jobs (exam_page_id);
            CREATE INDEX IF NOT EXISTS ix_generation_jobs_status       ON generation_jobs (status);
            CREATE INDEX IF NOT EXISTS ix_generation_jobs_created_at   ON generation_jobs (created_at DESC);
            """);

        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE  constraint_type = 'FOREIGN KEY'
                    AND    table_name      = 'generation_jobs'
                    AND    constraint_name = 'fk_generation_jobs_exam_page_id'
                ) THEN
                    ALTER TABLE generation_jobs
                        ADD CONSTRAINT fk_generation_jobs_exam_page_id
                        FOREIGN KEY (exam_page_id) REFERENCES exam_pages(id)
                        ON DELETE CASCADE;
                END IF;
            END; $$;
            """);

        // ── 26. question_drafts ───────────────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS question_drafts (
                id                       integer     GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                generation_job_id        integer     NOT NULL,
                subject_id               integer,
                topic_id                 integer,
                difficulty_level_id      integer,
                language                 varchar(10) NOT NULL DEFAULT 'en',
                question_text            text        NOT NULL DEFAULT '',
                options_json             jsonb       NOT NULL DEFAULT '[]',
                correct_index            integer     NOT NULL DEFAULT 0,
                explanation              text,
                calculation_steps_json   jsonb,
                difficulty_estimate      varchar(20),
                estimated_seconds        integer,
                flags_json               jsonb       NOT NULL DEFAULT '{}',
                status                   integer     NOT NULL DEFAULT 1,
                reviewer_id              uuid,
                review_notes             text,
                reviewed_at              timestamptz,
                approved_question_id     uuid,
                source_question_id       uuid,
                model                    varchar(100),
                prompt_template_version  integer,
                created_at               timestamptz NOT NULL DEFAULT now()
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS ix_question_drafts_job_id ON question_drafts (generation_job_id);
            CREATE INDEX IF NOT EXISTS ix_question_drafts_status ON question_drafts (status);
            """);

        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE  constraint_type = 'FOREIGN KEY'
                    AND    table_name      = 'question_drafts'
                    AND    constraint_name = 'fk_question_drafts_generation_job_id'
                ) THEN
                    ALTER TABLE question_drafts
                        ADD CONSTRAINT fk_question_drafts_generation_job_id
                        FOREIGN KEY (generation_job_id) REFERENCES generation_jobs(id)
                        ON DELETE CASCADE;
                END IF;
            END; $$;
            """);

        // ── 27. question_embeddings (raw SQL only — no EF entity) ─────────────
        // Uses pgvector. If the extension is not installed, wrapped in DO block.
        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'vector') THEN
                    -- Create table only when vector type is available
                    EXECUTE $q$
                        CREATE TABLE IF NOT EXISTS question_embeddings (
                            id          integer     GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            source_id   uuid        NOT NULL,
                            source      smallint    NOT NULL DEFAULT 1,
                            embedding   vector(768),
                            created_at  timestamptz NOT NULL DEFAULT now()
                        )
                    $q$;
                    -- HNSW index for fast cosine similarity search
                    EXECUTE $q$
                        CREATE INDEX IF NOT EXISTS ix_question_embeddings_vector
                        ON question_embeddings USING hnsw (embedding vector_cosine_ops)
                        WITH (m = 16, ef_construction = 64)
                    $q$;
                    -- Lookup index
                    EXECUTE $q$
                        CREATE INDEX IF NOT EXISTS ix_question_embeddings_source
                        ON question_embeddings (source_id, source)
                    $q$;
                END IF;
            END; $$;
            """);

        // ── 28. llm_usage ─────────────────────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS llm_usage (
                id                integer      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                date              date         NOT NULL,
                provider          varchar(50)  NOT NULL DEFAULT 'gemini',
                model             varchar(100) NOT NULL DEFAULT '',
                prompt_tokens     integer      NOT NULL DEFAULT 0,
                completion_tokens integer      NOT NULL DEFAULT 0,
                total_tokens      integer      NOT NULL DEFAULT 0,
                request_count     integer      NOT NULL DEFAULT 0,
                created_at        timestamptz  NOT NULL DEFAULT now(),
                updated_at        timestamptz  NOT NULL DEFAULT now()
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS ix_llm_usage_date_provider_model
                ON llm_usage (date, provider, model);
            """);

        // ── 29. question_reports ──────────────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS question_reports (
                id          integer     GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                question_id uuid        NOT NULL,
                reporter_id uuid,
                report_type varchar(50) NOT NULL DEFAULT 'other',
                report_text text        NOT NULL DEFAULT '',
                status      integer     NOT NULL DEFAULT 1,
                resolved_by uuid,
                resolved_at timestamptz,
                resolution  text,
                created_at  timestamptz NOT NULL DEFAULT now()
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS ix_question_reports_question_id ON question_reports (question_id);
            CREATE INDEX IF NOT EXISTS ix_question_reports_status       ON question_reports (status);
            """);

        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE  constraint_type = 'FOREIGN KEY'
                    AND    table_name      = 'question_reports'
                    AND    constraint_name = 'fk_question_reports_question_id'
                ) THEN
                    ALTER TABLE question_reports
                        ADD CONSTRAINT fk_question_reports_question_id
                        FOREIGN KEY (question_id) REFERENCES questions(id)
                        ON DELETE CASCADE;
                END IF;
            END; $$;
            """);
    }

    private static async Task SeedCareerQuizQuestionsAsync(AppDbContext db)
    {
        // Skip if any questions already exist (idempotent)
        if (await db.CareerQuizQuestions.AnyAsync()) return;

        // 5 default questions — each option maps to one of the 8 career categories.
        // Admins can edit, reorder, add, or delete these from the admin panel.
        var questions = new List<Entities.CareerGuide.CareerQuizQuestion>
        {
            new()
            {
                QuestionText = "It's a free Saturday afternoon. What sounds most appealing?",
                SortOrder    = 1,
                IsActive     = true,
                Options      =
                [
                    new() { OptionText = "Build or fix something with my hands — cook, craft, repair", CareerCategory = "makers",         SortOrder = 1 },
                    new() { OptionText = "Head outdoors — a hike, a park, anywhere open",              CareerCategory = "explorers",      SortOrder = 2 },
                    new() { OptionText = "Call friends, plan a gathering, or help someone",            CareerCategory = "connectors",     SortOrder = 3 },
                    new() { OptionText = "Work on a personal digital project or learn something new",  CareerCategory = "screen-workers", SortOrder = 4 },
                ],
            },
            new()
            {
                QuestionText = "Which kind of problem excites you the most?",
                SortOrder    = 2,
                IsActive     = true,
                Options      =
                [
                    new() { OptionText = "A complex puzzle or research challenge with a clever answer", CareerCategory = "thinkers",   SortOrder = 1 },
                    new() { OptionText = "A creative or performance challenge — music, design, stage",  CareerCategory = "performers", SortOrder = 2 },
                    new() { OptionText = "A people challenge — supporting or motivating someone",       CareerCategory = "healers",    SortOrder = 3 },
                    new() { OptionText = "A business challenge — building something from nothing",      CareerCategory = "builders",   SortOrder = 4 },
                ],
            },
            new()
            {
                QuestionText = "Which achievement would make you proudest five years from now?",
                SortOrder    = 3,
                IsActive     = true,
                Options      =
                [
                    new() { OptionText = "A handmade product in someone's home that I built myself",   CareerCategory = "makers",         SortOrder = 1 },
                    new() { OptionText = "A community or event I organised that people still mention", CareerCategory = "connectors",     SortOrder = 2 },
                    new() { OptionText = "A thriving business or product used by thousands",           CareerCategory = "builders",       SortOrder = 3 },
                    new() { OptionText = "Content I created that genuinely changed how people think",  CareerCategory = "screen-workers", SortOrder = 4 },
                ],
            },
            new()
            {
                QuestionText = "Which environment makes you feel most alive?",
                SortOrder    = 4,
                IsActive     = true,
                Options      =
                [
                    new() { OptionText = "Outdoors — trails, forests, fields, or open water",           CareerCategory = "explorers",  SortOrder = 1 },
                    new() { OptionText = "On stage, in front of a camera, or at a microphone",          CareerCategory = "performers", SortOrder = 2 },
                    new() { OptionText = "Surrounded by people I am genuinely helping recover or grow", CareerCategory = "healers",    SortOrder = 3 },
                    new() { OptionText = "A quiet desk with a hard problem and access to data",          CareerCategory = "thinkers",   SortOrder = 4 },
                ],
            },
            new()
            {
                QuestionText = "Which sentence feels most true to you right now?",
                SortOrder    = 5,
                IsActive     = true,
                Options      =
                [
                    new() { OptionText = "I want to perform, entertain, and move people",       CareerCategory = "performers", SortOrder = 1 },
                    new() { OptionText = "I want to care for and heal those around me",         CareerCategory = "healers",    SortOrder = 2 },
                    new() { OptionText = "I want to connect people and lead communities",       CareerCategory = "connectors", SortOrder = 3 },
                    new() { OptionText = "I want to build a product or business that lasts",   CareerCategory = "builders",   SortOrder = 4 },
                ],
            },
        };

        db.CareerQuizQuestions.AddRange(questions);
        await db.SaveChangesAsync();
    }
}

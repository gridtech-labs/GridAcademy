using GridAcademy.Data.Entities;
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
                Role         = "Admin",
                IsActive     = true
            });
            logger.LogInformation("Default admin seeded → {Email}", adminEmail);
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
}

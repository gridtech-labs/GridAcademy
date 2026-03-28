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
}

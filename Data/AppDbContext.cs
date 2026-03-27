using GridAcademy.Data.Entities;
using GridAcademy.Data.Entities.Assessment;
using GridAcademy.Data.Entities.Content;
using GridAcademy.Data.Entities.Exam;
using GridAcademy.Data.Entities.Marketplace;
using GridAcademy.Data.Entities.VideoLearning;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Users ──────────────────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();

    // ── Exam Module ────────────────────────────────────────────────────────
    public DbSet<ExamLevel>    ExamLevels    => Set<ExamLevel>();
    public DbSet<ExamPage>     ExamPages     => Set<ExamPage>();
    public DbSet<ExamPageTest> ExamPageTests => Set<ExamPageTest>();

    // ── Marketplace ────────────────────────────────────────────────────────
    public DbSet<MpProvider>    MpProviders    => Set<MpProvider>();
    public DbSet<MpTestSeries>  MpTestSeries   => Set<MpTestSeries>();
    public DbSet<MpSeriesTest>  MpSeriesTests  => Set<MpSeriesTest>();
    public DbSet<MpOrder>       MpOrders       => Set<MpOrder>();
    public DbSet<MpEntitlement> MpEntitlements => Set<MpEntitlement>();
    public DbSet<MpCommission>  MpCommissions  => Set<MpCommission>();
    public DbSet<MpPayout>      MpPayouts      => Set<MpPayout>();
    public DbSet<MpReview>      MpReviews      => Set<MpReview>();
    public DbSet<MpPromoCode>   MpPromoCodes   => Set<MpPromoCode>();
    public DbSet<MpCmsBanner>   MpCmsBanners   => Set<MpCmsBanner>();
    public DbSet<MpOtpSession>  MpOtpSessions  => Set<MpOtpSession>();

    // ── Content — master tables ────────────────────────────────────────────
    public DbSet<QuestionTypeMaster>   QuestionTypes         => Set<QuestionTypeMaster>();
    public DbSet<Subject>              Subjects              => Set<Subject>();
    public DbSet<Topic>                Topics                => Set<Topic>();
    public DbSet<DifficultyLevel>      DifficultyLevels      => Set<DifficultyLevel>();
    public DbSet<ComplexityLevel>      ComplexityLevels      => Set<ComplexityLevel>();
    public DbSet<MarksMaster>          MarksMaster           => Set<MarksMaster>();
    public DbSet<NegativeMarksMaster>  NegativeMarksMaster   => Set<NegativeMarksMaster>();
    public DbSet<ExamType>             ExamTypes             => Set<ExamType>();
    public DbSet<Tag>                  Tags                  => Set<Tag>();

    // ── Content — questions ────────────────────────────────────────────────
    public DbSet<Question>             Questions             => Set<Question>();
    public DbSet<QuestionOption>       QuestionOptions       => Set<QuestionOption>();
    public DbSet<QuestionTag>          QuestionTags          => Set<QuestionTag>();
    public DbSet<QuestionBlank>        QuestionBlanks        => Set<QuestionBlank>();
    public DbSet<QuestionPassage>      QuestionPassages      => Set<QuestionPassage>();
    public DbSet<QuestionMatchItem>    QuestionMatchItems    => Set<QuestionMatchItem>();
    public DbSet<QuestionMatchCorrect> QuestionMatchCorrects => Set<QuestionMatchCorrect>();

    // ── Assessment ─────────────────────────────────────────────────────────────
    public DbSet<Test>                  Tests                 => Set<Test>();
    public DbSet<TestSection>           TestSections          => Set<TestSection>();
    public DbSet<TestAssignment>        TestAssignments       => Set<TestAssignment>();
    public DbSet<TestAttempt>           TestAttempts          => Set<TestAttempt>();
    public DbSet<AttemptQuestion>       AttemptQuestions      => Set<AttemptQuestion>();
    public DbSet<AttemptAnswer>         AttemptAnswers        => Set<AttemptAnswer>();
    public DbSet<AttemptSectionResult>  AttemptSectionResults => Set<AttemptSectionResult>();

    // ── Video Learning ──────────────────────────────────────────
    public DbSet<VlDomain>             VlDomains             => Set<VlDomain>();
    public DbSet<VlVideoCategory>      VlVideoCategories     => Set<VlVideoCategory>();
    public DbSet<VlVideo>              VlVideos              => Set<VlVideo>();
    public DbSet<VlLearningPath>     VlLearningPaths     => Set<VlLearningPath>();
    public DbSet<VlLearningPathNode> VlLearningPathNodes => Set<VlLearningPathNode>();
    public DbSet<VlContentFile>      VlContentFiles      => Set<VlContentFile>();
    public DbSet<VlProgram>            VlPrograms            => Set<VlProgram>();
    public DbSet<VlProgramLearningPath> VlProgramLearningPaths => Set<VlProgramLearningPath>();
    public DbSet<VlProgramPricingPlan> VlProgramPricingPlans => Set<VlProgramPricingPlan>();
    public DbSet<VlCoupon>             VlCoupons             => Set<VlCoupon>();
    public DbSet<VlSalesChannel>       VlSalesChannels       => Set<VlSalesChannel>();
    public DbSet<VlChannelProgramPrice> VlChannelProgramPrices => Set<VlChannelProgramPrice>();
    public DbSet<VlEnrollment>         VlEnrollments         => Set<VlEnrollment>();
    public DbSet<VlVideoProgress>      VlVideoProgresses     => Set<VlVideoProgress>();
    public DbSet<VlCourseLaunch>       VlCourseLaunches      => Set<VlCourseLaunch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ──────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasColumnName("id");
            entity.Property(u => u.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            entity.Property(u => u.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            entity.Property(u => u.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(u => u.Role).HasColumnName("role").HasMaxLength(50).HasDefaultValue("User");
            entity.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(u => u.CreatedAt).HasColumnName("created_at");
            entity.Property(u => u.UpdatedAt).HasColumnName("updated_at");
            entity.Property(u => u.LastLoginAt).HasColumnName("last_login_at");
            entity.Ignore(u => u.FullName);
        });

        // ── Master base helper ─────────────────────────────────────────────
        static void ConfigureMaster<T>(
            Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> e,
            string table)
            where T : MasterBase
        {
            e.ToTable(table);
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        }

        modelBuilder.Entity<Subject>(e        => ConfigureMaster(e, "subjects"));
        modelBuilder.Entity<DifficultyLevel>(e => ConfigureMaster(e, "difficulty_levels"));
        modelBuilder.Entity<ComplexityLevel>(e => ConfigureMaster(e, "complexity_levels"));
        modelBuilder.Entity<ExamType>(e        => ConfigureMaster(e, "exam_types"));
        modelBuilder.Entity<Tag>(e             => ConfigureMaster(e, "tags"));

        modelBuilder.Entity<Topic>(e =>
        {
            ConfigureMaster(e, "topics");
            e.Property(x => x.SubjectId).HasColumnName("subject_id");
            e.HasOne(x => x.Subject).WithMany(s => s.Topics).HasForeignKey(x => x.SubjectId);
        });

        modelBuilder.Entity<MarksMaster>(e =>
        {
            ConfigureMaster(e, "marks_master");
            e.Property(x => x.Value).HasColumnName("value").HasColumnType("numeric(5,2)");
        });

        modelBuilder.Entity<NegativeMarksMaster>(e =>
        {
            ConfigureMaster(e, "negative_marks_master");
            e.Property(x => x.Value).HasColumnName("value").HasColumnType("numeric(5,2)");
        });

        // ── QuestionTypeMaster ─────────────────────────────────────────────
        // IDs are seeded to match QuestionType enum values — NOT auto-generated.
        modelBuilder.Entity<QuestionTypeMaster>(e =>
        {
            e.ToTable("question_types");
            e.HasKey(x => x.Id);
            // Store the enum PK as int; ValueGeneratedNever because IDs are seeded to match the enum.
            e.Property(x => x.Id).HasColumnName("id").HasConversion<int>().ValueGeneratedNever();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(x => x.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ix_question_types_code");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        });

        // ── QuestionPassage ────────────────────────────────────────────────
        modelBuilder.Entity<QuestionPassage>(e =>
        {
            e.ToTable("question_passages");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.Title).HasColumnName("title").HasMaxLength(300);
            e.Property(p => p.PassageText).HasColumnName("passage_text").IsRequired();
            e.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        });

        // ── Question ───────────────────────────────────────────────────────
        modelBuilder.Entity<Question>(e =>
        {
            e.ToTable("questions");
            e.HasKey(q => q.Id);
            e.Property(q => q.Id).HasColumnName("id");
            e.Property(q => q.Text).HasColumnName("text").IsRequired();
            e.Property(q => q.Solution).HasColumnName("solution");
            e.Property(q => q.Subtopic).HasColumnName("subtopic").HasMaxLength(200);

            // QuestionType enum stored as int, FK → question_types.id
            e.Property(q => q.QuestionType)
             .HasColumnName("question_type_id")
             .HasConversion<int>();

            e.HasOne(q => q.QuestionTypeMaster)
             .WithMany(qt => qt.Questions)
             .HasForeignKey(q => q.QuestionType)
             .HasConstraintName("fk_questions_question_type_id")
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(q => q.Status)
             .HasColumnName("status")
             .HasConversion<int>();

            // Classification FKs
            e.Property(q => q.SubjectId).HasColumnName("subject_id");
            e.Property(q => q.TopicId).HasColumnName("topic_id");
            e.Property(q => q.DifficultyLevelId).HasColumnName("difficulty_level_id");
            e.Property(q => q.ComplexityLevelId).HasColumnName("complexity_level_id");
            e.Property(q => q.MarksId).HasColumnName("marks_id");
            e.Property(q => q.NegativeMarksId).HasColumnName("negative_marks_id");
            e.Property(q => q.ExamTypeId).HasColumnName("exam_type_id");

            // Type-specific nullable columns
            e.Property(q => q.NumericalAnswer).HasColumnName("numerical_answer").HasColumnType("numeric(12,4)");
            e.Property(q => q.NumericalTolerance).HasColumnName("numerical_tolerance").HasColumnType("numeric(12,4)");
            e.Property(q => q.AssertionText).HasColumnName("assertion_text");
            e.Property(q => q.ReasonText).HasColumnName("reason_text");
            e.Property(q => q.PassageId).HasColumnName("passage_id");

            // Audit
            e.Property(q => q.CreatedAt).HasColumnName("created_at");
            e.Property(q => q.UpdatedAt).HasColumnName("updated_at");
            e.Property(q => q.CreatedBy).HasColumnName("created_by");
            e.Property(q => q.UpdatedBy).HasColumnName("updated_by");

            // Navigations
            e.HasOne(q => q.Subject).WithMany(s => s.Questions).HasForeignKey(q => q.SubjectId);
            e.HasOne(q => q.Topic).WithMany(t => t.Questions).HasForeignKey(q => q.TopicId);
            e.HasOne(q => q.DifficultyLevel).WithMany(d => d.Questions).HasForeignKey(q => q.DifficultyLevelId);
            e.HasOne(q => q.ComplexityLevel).WithMany(c => c.Questions).HasForeignKey(q => q.ComplexityLevelId);
            e.HasOne(q => q.Marks).WithMany(m => m.Questions).HasForeignKey(q => q.MarksId);
            e.HasOne(q => q.NegativeMarks).WithMany(n => n.Questions).HasForeignKey(q => q.NegativeMarksId);
            e.HasOne(q => q.ExamType).WithMany(et => et.Questions).HasForeignKey(q => q.ExamTypeId);
            e.HasOne(q => q.Passage).WithMany(p => p.Questions)
             .HasForeignKey(q => q.PassageId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);

            // Performance indexes
            e.HasIndex(q => q.SubjectId).HasDatabaseName("ix_questions_subject_id");
            e.HasIndex(q => q.DifficultyLevelId).HasDatabaseName("ix_questions_difficulty_level_id");
            e.HasIndex(q => q.ExamTypeId).HasDatabaseName("ix_questions_exam_type_id");
            e.HasIndex(q => q.Status).HasDatabaseName("ix_questions_status");
            e.HasIndex(q => q.QuestionType).HasDatabaseName("ix_questions_question_type_id");
            e.HasIndex(q => q.PassageId).HasDatabaseName("ix_questions_passage_id");
        });

        // ── QuestionOption ─────────────────────────────────────────────────
        modelBuilder.Entity<QuestionOption>(e =>
        {
            e.ToTable("question_options");
            e.HasKey(o => o.Id);
            e.Property(o => o.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(o => o.QuestionId).HasColumnName("question_id");
            e.Property(o => o.Label).HasColumnName("label").HasMaxLength(1);
            e.Property(o => o.Text).HasColumnName("text").IsRequired();
            e.Property(o => o.IsCorrect).HasColumnName("is_correct").HasDefaultValue(false);
            e.Property(o => o.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            e.HasOne(o => o.Question).WithMany(q => q.Options)
             .HasForeignKey(o => o.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── QuestionTag (junction) ─────────────────────────────────────────
        modelBuilder.Entity<QuestionTag>(e =>
        {
            e.ToTable("question_tags");
            e.HasKey(qt => new { qt.QuestionId, qt.TagId });
            e.Property(qt => qt.QuestionId).HasColumnName("question_id");
            e.Property(qt => qt.TagId).HasColumnName("tag_id");
            e.HasOne(qt => qt.Question).WithMany(q => q.QuestionTags)
             .HasForeignKey(qt => qt.QuestionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(qt => qt.Tag).WithMany(t => t.QuestionTags)
             .HasForeignKey(qt => qt.TagId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── QuestionBlank (FillInBlanks) ────────────────────────────────────
        modelBuilder.Entity<QuestionBlank>(e =>
        {
            e.ToTable("question_blanks");
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(b => b.QuestionId).HasColumnName("question_id");
            e.Property(b => b.BlankIndex).HasColumnName("blank_index");
            e.Property(b => b.CorrectAnswer).HasColumnName("correct_answer").HasMaxLength(500).IsRequired();
            e.Property(b => b.AlternateAnswers).HasColumnName("alternate_answers");
            e.Property(b => b.CaseSensitive).HasColumnName("case_sensitive").HasDefaultValue(false);
            e.HasOne(b => b.Question).WithMany(q => q.Blanks)
             .HasForeignKey(b => b.QuestionId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(b => new { b.QuestionId, b.BlankIndex })
             .IsUnique().HasDatabaseName("ix_question_blanks_question_blank");
        });

        // ── QuestionMatchItem (MatchTheFollowing + MatrixMatch) ─────────────
        modelBuilder.Entity<QuestionMatchItem>(e =>
        {
            e.ToTable("question_match_items");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(i => i.QuestionId).HasColumnName("question_id");
            e.Property(i => i.ColumnSide).HasColumnName("column_side").HasMaxLength(10).IsRequired();
            e.Property(i => i.Label).HasColumnName("label").HasMaxLength(10).IsRequired();
            e.Property(i => i.Text).HasColumnName("text").IsRequired();
            e.Property(i => i.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            e.HasOne(i => i.Question).WithMany(q => q.MatchItems)
             .HasForeignKey(i => i.QuestionId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(i => i.QuestionId).HasDatabaseName("ix_question_match_items_question_id");
        });

        // ── QuestionMatchCorrect (MatchTheFollowing + MatrixMatch) ──────────
        modelBuilder.Entity<QuestionMatchCorrect>(e =>
        {
            e.ToTable("question_match_correct");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(c => c.QuestionId).HasColumnName("question_id");
            e.Property(c => c.LeftLabel).HasColumnName("left_label").HasMaxLength(10).IsRequired();
            e.Property(c => c.RightLabel).HasColumnName("right_label").HasMaxLength(10).IsRequired();
            e.HasOne(c => c.Question).WithMany(q => q.MatchCorrect)
             .HasForeignKey(c => c.QuestionId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(c => new { c.QuestionId, c.LeftLabel, c.RightLabel })
             .IsUnique().HasDatabaseName("ix_question_match_correct_unique");
        });

        // ── Test ───────────────────────────────────────────────────────────────────
        modelBuilder.Entity<Test>(e =>
        {
            e.ToTable("tests");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("id");
            e.Property(t => t.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
            e.Property(t => t.Instructions).HasColumnName("instructions");
            e.Property(t => t.DurationMinutes).HasColumnName("duration_minutes");
            e.Property(t => t.PassingPercent).HasColumnName("passing_percent").HasColumnType("numeric(5,2)");
            e.Property(t => t.NegativeMarkingEnabled).HasColumnName("negative_marking_enabled").HasDefaultValue(false);
            e.Property(t => t.ExamTypeId).HasColumnName("exam_type_id");
            e.Property(t => t.Status).HasColumnName("status").HasConversion<int>().HasDefaultValue(TestStatus.Draft);
            e.Property(t => t.CreatedAt).HasColumnName("created_at");
            e.Property(t => t.UpdatedAt).HasColumnName("updated_at");
            e.Property(t => t.CreatedBy).HasColumnName("created_by");
            e.Property(t => t.UpdatedBy).HasColumnName("updated_by");
            e.HasOne(t => t.ExamType).WithMany().HasForeignKey(t => t.ExamTypeId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(t => t.Status).HasDatabaseName("ix_tests_status");
            e.HasIndex(t => t.ExamTypeId).HasDatabaseName("ix_tests_exam_type_id");
        });

        // ── TestSection ────────────────────────────────────────────────────────────
        modelBuilder.Entity<TestSection>(e =>
        {
            e.ToTable("test_sections");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(s => s.TestId).HasColumnName("test_id");
            e.Property(s => s.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(s => s.SubjectId).HasColumnName("subject_id");
            e.Property(s => s.DifficultyLevelId).HasColumnName("difficulty_level_id");
            e.Property(s => s.QuestionCount).HasColumnName("question_count");
            e.Property(s => s.MarksPerQuestion).HasColumnName("marks_per_question").HasColumnType("numeric(5,2)");
            e.Property(s => s.NegativeMarksPerQuestion).HasColumnName("negative_marks_per_question").HasColumnType("numeric(5,2)");
            e.Property(s => s.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            e.HasOne(s => s.Test).WithMany(t => t.Sections).HasForeignKey(s => s.TestId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Subject).WithMany().HasForeignKey(s => s.SubjectId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.DifficultyLevel).WithMany().HasForeignKey(s => s.DifficultyLevelId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(s => new { s.TestId, s.SortOrder }).HasDatabaseName("ix_test_sections_test_sort");
        });

        // ── TestAssignment ─────────────────────────────────────────────────────────
        modelBuilder.Entity<TestAssignment>(e =>
        {
            e.ToTable("test_assignments");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.TestId).HasColumnName("test_id");
            e.Property(a => a.StudentId).HasColumnName("student_id");
            e.Property(a => a.AvailableFrom).HasColumnName("available_from");
            e.Property(a => a.AvailableTo).HasColumnName("available_to");
            e.Property(a => a.MaxAttempts).HasColumnName("max_attempts").HasDefaultValue(1);
            e.Property(a => a.AssignedAt).HasColumnName("assigned_at");
            e.Property(a => a.AssignedBy).HasColumnName("assigned_by");
            e.HasOne(a => a.Test).WithMany(t => t.Assignments).HasForeignKey(a => a.TestId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Student).WithMany().HasForeignKey(a => a.StudentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => new { a.TestId, a.StudentId }).IsUnique().HasDatabaseName("ix_test_assignments_test_student");
            e.HasIndex(a => a.StudentId).HasDatabaseName("ix_test_assignments_student_id");
        });

        // ── TestAttempt ────────────────────────────────────────────────────────────
        modelBuilder.Entity<TestAttempt>(e =>
        {
            e.ToTable("test_attempts");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.AssignmentId).HasColumnName("assignment_id");
            e.Property(a => a.StudentId).HasColumnName("student_id");
            e.Property(a => a.TestId).HasColumnName("test_id");
            e.Property(a => a.AttemptNumber).HasColumnName("attempt_number");
            e.Property(a => a.Status).HasColumnName("status").HasConversion<int>();
            e.Property(a => a.StartedAt).HasColumnName("started_at");
            e.Property(a => a.SubmittedAt).HasColumnName("submitted_at");
            e.Property(a => a.DurationSecondsUsed).HasColumnName("duration_seconds_used").HasDefaultValue(0);
            e.Property(a => a.TotalMarksObtained).HasColumnName("total_marks_obtained").HasColumnType("numeric(8,2)");
            e.Property(a => a.TotalMarksPossible).HasColumnName("total_marks_possible").HasColumnType("numeric(8,2)");
            e.Property(a => a.Percentage).HasColumnName("percentage").HasColumnType("numeric(5,2)");
            e.Property(a => a.IsPassed).HasColumnName("is_passed");
            e.Property(a => a.ViolationLog).HasColumnName("violation_log").HasMaxLength(8000);
            e.HasOne(a => a.Assignment).WithMany(ta => ta.Attempts).HasForeignKey(a => a.AssignmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => new { a.StudentId, a.TestId }).HasDatabaseName("ix_test_attempts_student_test");
            e.HasIndex(a => new { a.AssignmentId, a.AttemptNumber }).IsUnique().HasDatabaseName("ix_test_attempts_assignment_num");
            e.HasIndex(a => a.Status).HasDatabaseName("ix_test_attempts_status");
        });

        // ── AttemptQuestion ────────────────────────────────────────────────────────
        modelBuilder.Entity<AttemptQuestion>(e =>
        {
            e.ToTable("attempt_questions");
            e.HasKey(q => q.Id);
            e.Property(q => q.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(q => q.AttemptId).HasColumnName("attempt_id");
            e.Property(q => q.QuestionId).HasColumnName("question_id");
            e.Property(q => q.SectionIndex).HasColumnName("section_index");
            e.Property(q => q.SectionName).HasColumnName("section_name").HasMaxLength(200);
            e.Property(q => q.DisplayOrder).HasColumnName("display_order");
            e.Property(q => q.DisplayOrderInSection).HasColumnName("display_order_in_section");
            e.Property(q => q.MarksForCorrect).HasColumnName("marks_for_correct").HasColumnType("numeric(5,2)");
            e.Property(q => q.NegativeMarks).HasColumnName("negative_marks").HasColumnType("numeric(5,2)");
            e.Property(q => q.IsVisited).HasColumnName("is_visited").HasDefaultValue(false);
            e.Property(q => q.IsMarkedForReview).HasColumnName("is_marked_for_review").HasDefaultValue(false);
            e.HasOne(q => q.Attempt).WithMany(a => a.Questions).HasForeignKey(q => q.AttemptId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(q => q.Question).WithMany().HasForeignKey(q => q.QuestionId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(q => new { q.AttemptId, q.QuestionId }).IsUnique().HasDatabaseName("ix_attempt_questions_attempt_question");
            e.HasIndex(q => new { q.AttemptId, q.DisplayOrder }).HasDatabaseName("ix_attempt_questions_attempt_order");
        });

        // ── AttemptAnswer ──────────────────────────────────────────────────────────
        modelBuilder.Entity<AttemptAnswer>(e =>
        {
            e.ToTable("attempt_answers");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(a => a.AttemptId).HasColumnName("attempt_id");
            e.Property(a => a.QuestionId).HasColumnName("question_id");
            e.Property(a => a.SelectedOptionIds).HasColumnName("selected_option_ids").HasMaxLength(500);
            e.Property(a => a.NumericalValue).HasColumnName("numerical_value").HasColumnType("numeric(12,4)");
            e.Property(a => a.IsClear).HasColumnName("is_clear").HasDefaultValue(false);
            e.Property(a => a.SavedAt).HasColumnName("saved_at");
            e.HasOne(a => a.Attempt).WithMany(ta => ta.Answers).HasForeignKey(a => a.AttemptId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => new { a.AttemptId, a.QuestionId }).IsUnique().HasDatabaseName("ix_attempt_answers_attempt_question");
        });

        // ── AttemptSectionResult ───────────────────────────────────────────────────
        modelBuilder.Entity<AttemptSectionResult>(e =>
        {
            e.ToTable("attempt_section_results");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(r => r.AttemptId).HasColumnName("attempt_id");
            e.Property(r => r.SectionIndex).HasColumnName("section_index");
            e.Property(r => r.SectionName).HasColumnName("section_name").HasMaxLength(200);
            e.Property(r => r.TotalQuestions).HasColumnName("total_questions");
            e.Property(r => r.Attempted).HasColumnName("attempted");
            e.Property(r => r.Correct).HasColumnName("correct");
            e.Property(r => r.Incorrect).HasColumnName("incorrect");
            e.Property(r => r.Unattempted).HasColumnName("unattempted");
            e.Property(r => r.MarksObtained).HasColumnName("marks_obtained").HasColumnType("numeric(8,2)");
            e.Property(r => r.MaxMarks).HasColumnName("max_marks").HasColumnType("numeric(8,2)");
            e.HasOne(r => r.Attempt).WithMany(a => a.SectionResults).HasForeignKey(r => r.AttemptId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(r => r.AttemptId).HasDatabaseName("ix_attempt_section_results_attempt_id");
        });

        // ──────────────────────────────────────────────────────────────
        // Video Learning Module
        // ──────────────────────────────────────────────────────────────
        var mb = modelBuilder;

        mb.Entity<VlDomain>(e => {
            e.ToTable("vl_domains");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description);
            e.Property(x => x.LogoUrl).HasMaxLength(500);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        });

        mb.Entity<VlVideoCategory>(e => {
            e.ToTable("vl_video_categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.HasOne(x => x.Domain).WithMany(d => d.VideoCategories)
                .HasForeignKey(x => x.DomainId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.DomainId).HasDatabaseName("ix_vl_video_categories_domain_id");
        });

        mb.Entity<VlVideo>(e => {
            e.ToTable("vl_videos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(300);
            e.Property(x => x.FilePath).HasMaxLength(500);
            e.Property(x => x.ThumbnailPath).HasMaxLength(500);
            e.Property(x => x.OriginalFileName).HasMaxLength(255);
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
            e.HasOne(x => x.Domain).WithMany(d => d.Videos)
                .HasForeignKey(x => x.DomainId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.Videos)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.DomainId).HasDatabaseName("ix_vl_videos_domain_id");
            e.HasIndex(x => x.CategoryId).HasDatabaseName("ix_vl_videos_category_id");
            e.HasIndex(x => x.Status).HasDatabaseName("ix_vl_videos_status");
        });

        mb.Entity<VlLearningPath>(e => {
            e.ToTable("vl_learning_paths");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(300);
            e.Property(x => x.ThumbnailPath).HasMaxLength(500);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
            e.HasOne(x => x.Domain).WithMany(d => d.LearningPaths)
                .HasForeignKey(x => x.DomainId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.DomainId).HasDatabaseName("ix_vl_learning_paths_domain_id");
        });

        mb.Entity<VlProgram>(e => {
            e.ToTable("vl_programs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(300);
            e.Property(x => x.LearningCode).HasMaxLength(50);
            e.Property(x => x.IsBlendedLearning).HasDefaultValue(false);
            e.Property(x => x.ShortDescription).HasMaxLength(500);
            e.Property(x => x.ThumbnailPath).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
            e.HasOne(x => x.Domain).WithMany(d => d.Programs)
                .HasForeignKey(x => x.DomainId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.DomainId).HasDatabaseName("ix_vl_programs_domain_id");
            e.HasIndex(x => x.Status).HasDatabaseName("ix_vl_programs_status");
        });

        mb.Entity<VlProgramLearningPath>(e => {
            e.ToTable("vl_program_learning_paths");
            e.HasKey(x => new { x.ProgramId, x.LearningPathId });
            e.HasOne(x => x.Program).WithMany(p => p.ProgramLearningPaths)
                .HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.LearningPath).WithMany(lp => lp.ProgramLearningPaths)
                .HasForeignKey(x => x.LearningPathId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<VlProgramPricingPlan>(e => {
            e.ToTable("vl_program_pricing_plans");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.PriceInr).HasColumnType("numeric(12,2)");
            e.Property(x => x.PriceUsd).HasColumnType("numeric(12,2)");
            e.Property(x => x.OriginalPriceInr).HasColumnType("numeric(12,2)");
            e.Property(x => x.OriginalPriceUsd).HasColumnType("numeric(12,2)");
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.HasOne(x => x.Program).WithMany(p => p.PricingPlans)
                .HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ProgramId).HasDatabaseName("ix_vl_ppp_program_id");
        });

        mb.Entity<VlCoupon>(e => {
            e.ToTable("vl_coupons");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(300);
            e.Property(x => x.DiscountType).HasConversion<int>();
            e.Property(x => x.DiscountValue).HasColumnType("numeric(10,2)");
            e.Property(x => x.MaxDiscountInr).HasColumnType("numeric(12,2)");
            e.Property(x => x.MaxDiscountUsd).HasColumnType("numeric(12,2)");
            e.Property(x => x.ValidFrom).HasColumnType("timestamptz");
            e.Property(x => x.ValidTo).HasColumnType("timestamptz");
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
            e.HasOne(x => x.Program).WithMany(p => p.Coupons)
                .HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
            e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ix_vl_coupons_code");
            e.HasIndex(x => x.ProgramId).HasDatabaseName("ix_vl_coupons_program_id");
        });

        mb.Entity<VlSalesChannel>(e => {
            e.ToTable("vl_sales_channels");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.BaseUrl).HasMaxLength(500);
            e.Property(x => x.ApiKeyHash).IsRequired().HasMaxLength(64);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
            e.HasIndex(x => x.ApiKeyHash).HasDatabaseName("ix_vl_sales_channels_api_key_hash");
        });

        mb.Entity<VlChannelProgramPrice>(e => {
            e.ToTable("vl_channel_program_prices");
            e.HasKey(x => x.Id);
            e.Property(x => x.OverridePriceInr).HasColumnType("numeric(12,2)");
            e.Property(x => x.OverridePriceUsd).HasColumnType("numeric(12,2)");
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.HasOne(x => x.Channel).WithMany(c => c.ChannelProgramPrices)
                .HasForeignKey(x => x.ChannelId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.PricingPlan).WithMany(p => p.ChannelPrices)
                .HasForeignKey(x => x.PricingPlanId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ChannelId, x.PricingPlanId }).IsUnique()
                .HasDatabaseName("ix_vl_cpp_channel_plan");
        });

        mb.Entity<VlEnrollment>(e => {
            e.ToTable("vl_enrollments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.AmountPaidInr).HasColumnType("numeric(12,2)");
            e.Property(x => x.AmountPaidUsd).HasColumnType("numeric(12,2)");
            e.Property(x => x.DiscountApplied).HasColumnType("numeric(12,2)");
            e.Property(x => x.CouponCode).HasMaxLength(50);
            e.Property(x => x.EnrolledAt).HasColumnType("timestamptz");
            e.Property(x => x.ExpiresAt).HasColumnType("timestamptz");
            e.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Program).WithMany(p => p.Enrollments)
                .HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PricingPlan).WithMany(p => p.Enrollments)
                .HasForeignKey(x => x.PricingPlanId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Channel).WithMany(c => c.Enrollments)
                .HasForeignKey(x => x.ChannelId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
            e.HasIndex(x => x.UserId).HasDatabaseName("ix_vl_enrollments_user_id");
            e.HasIndex(x => x.ProgramId).HasDatabaseName("ix_vl_enrollments_program_id");
            e.HasIndex(x => x.Status).HasDatabaseName("ix_vl_enrollments_status");
            // Partial unique index: one active enrollment per user-program
            e.HasIndex(x => new { x.UserId, x.ProgramId })
                .IsUnique().HasFilter("\"Status\" = 0")
                .HasDatabaseName("ix_vl_enrollments_user_program_active");
        });

        mb.Entity<VlVideoProgress>(e => {
            e.ToTable("vl_video_progress");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.CompletedAt).HasColumnType("timestamptz");
            e.Property(x => x.LastUpdatedAt).HasColumnType("timestamptz");
            e.HasOne(x => x.Enrollment).WithMany(en => en.VideoProgresses)
                .HasForeignKey(x => x.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Video).WithMany(v => v.Progresses)
                .HasForeignKey(x => x.VideoId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.EnrollmentId, x.VideoId }).IsUnique()
                .HasDatabaseName("ix_vl_vp_enrollment_video");
            e.HasIndex(x => x.EnrollmentId).HasDatabaseName("ix_vl_vp_enrollment_id");
        });

        mb.Entity<VlContentFile>(e => {
            e.ToTable("vl_content_files");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(300);
            e.Property(x => x.FilePath).HasMaxLength(500);
            e.Property(x => x.OriginalFileName).HasMaxLength(255);
            e.Property(x => x.ContentType).HasConversion<int>();
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
            e.HasOne(x => x.Domain).WithMany()
                .HasForeignKey(x => x.DomainId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.DomainId).HasDatabaseName("ix_vl_content_files_domain_id");
            e.HasIndex(x => x.ContentType).HasDatabaseName("ix_vl_content_files_type");
        });

        mb.Entity<VlLearningPathNode>(e => {
            e.ToTable("vl_learning_path_nodes");
            e.HasKey(x => x.Id);
            e.Property(x => x.NodeType).IsRequired().HasMaxLength(2);
            e.Property(x => x.Title).IsRequired().HasMaxLength(500);
            e.Property(x => x.IsPreview).HasDefaultValue(false);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
            // Parent LP
            e.HasOne(x => x.LearningPath).WithMany(lp => lp.Nodes)
                .HasForeignKey(x => x.LearningPathId).OnDelete(DeleteBehavior.Cascade);
            // Self-referencing parent node (cascade to delete children when module is deleted)
            e.HasOne(x => x.ParentNode).WithMany(n => n.ChildNodes)
                .HasForeignKey(x => x.ParentNodeId).OnDelete(DeleteBehavior.Cascade).IsRequired(false);
            e.HasIndex(x => x.LearningPathId).HasDatabaseName("ix_vl_lpn_learning_path_id");
            e.HasIndex(x => x.ParentNodeId).HasDatabaseName("ix_vl_lpn_parent_node_id");
            e.HasIndex(x => x.NodeType).HasDatabaseName("ix_vl_lpn_node_type");
            e.HasIndex(x => x.ContentId).HasDatabaseName("ix_vl_lpn_content_id");
        });

        mb.Entity<VlCourseLaunch>(e => {
            e.ToTable("vl_course_launches");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.BlockedReason).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.StartDate).HasColumnType("timestamptz");
            e.Property(x => x.EndDate).HasColumnType("timestamptz");
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
            e.HasOne(x => x.Program).WithMany(p => p.CourseLaunches)
                .HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ProgramId).HasDatabaseName("ix_vl_cl_program_id");
        });

        // ── Marketplace ────────────────────────────────────────────────────────

        modelBuilder.Entity<MpProvider>(e => {
            e.ToTable("mp_providers");
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
            e.Property(x => x.AgreedAt).HasColumnType("timestamptz");
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("ix_mp_providers_user_id");
        });

        modelBuilder.Entity<MpTestSeries>(e => {
            e.ToTable("mp_test_series");
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.SeriesType).HasConversion<int>();
            e.Property(x => x.PriceInr).HasColumnType("numeric(12,2)");
            e.Property(x => x.AvgRating).HasColumnType("numeric(3,2)");
            e.Property(x => x.PublishedAt).HasColumnType("timestamptz");
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
            e.HasOne(x => x.Provider).WithMany(p => p.TestSeries).HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ExamType).WithMany().HasForeignKey(x => x.ExamTypeId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.Slug).IsUnique().HasFilter("\"Status\" = 2").HasDatabaseName("ix_mp_series_slug_published");
            e.HasIndex(x => x.ExamTypeId).HasDatabaseName("ix_mp_series_exam_type");
            e.HasIndex(x => x.ProviderId).HasDatabaseName("ix_mp_series_provider");
            e.HasIndex(x => x.Status).HasDatabaseName("ix_mp_series_status");
        });

        modelBuilder.Entity<MpSeriesTest>(e => {
            e.ToTable("mp_series_tests");
            e.HasOne(x => x.Series).WithMany(s => s.SeriesTests).HasForeignKey(x => x.SeriesId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Test).WithMany().HasForeignKey(x => x.TestId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(new[] { "SeriesId", "TestId" }).IsUnique().HasDatabaseName("ix_mp_series_tests_unique");
        });

        modelBuilder.Entity<MpOrder>(e => {
            e.ToTable("mp_orders");
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.AmountInr).HasColumnType("numeric(12,2)");
            e.Property(x => x.GstAmount).HasColumnType("numeric(12,2)");
            e.Property(x => x.BookingFee).HasColumnType("numeric(12,2)");
            e.Property(x => x.GrandTotal).HasColumnType("numeric(12,2)");
            e.Property(x => x.DiscountApplied).HasColumnType("numeric(12,2)");
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
            e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Series).WithMany(s => s.Orders).HasForeignKey(x => x.SeriesId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.StudentId).HasDatabaseName("ix_mp_orders_student");
            e.HasIndex(x => x.BookingRef).IsUnique().HasDatabaseName("ix_mp_orders_booking_ref");
            e.HasIndex(x => x.RazorpayOrderId).HasDatabaseName("ix_mp_orders_razorpay");
        });

        modelBuilder.Entity<MpEntitlement>(e => {
            e.ToTable("mp_entitlements");
            e.Property(x => x.GrantedAt).HasColumnType("timestamptz");
            e.Property(x => x.ExpiresAt).HasColumnType("timestamptz");
            e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Series).WithMany(s => s.Entitlements).HasForeignKey(x => x.SeriesId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Order).WithOne(o => o.Entitlement).HasForeignKey<MpEntitlement>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(new[] { "StudentId", "SeriesId" }).HasDatabaseName("ix_mp_entitlements_student_series");
        });

        modelBuilder.Entity<MpCommission>(e => {
            e.ToTable("mp_commissions");
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.GrossAmount).HasColumnType("numeric(12,2)");
            e.Property(x => x.PlatformPct).HasColumnType("numeric(5,2)");
            e.Property(x => x.PlatformAmount).HasColumnType("numeric(12,2)");
            e.Property(x => x.ProviderPct).HasColumnType("numeric(5,2)");
            e.Property(x => x.ProviderAmount).HasColumnType("numeric(12,2)");
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.HasOne(x => x.Order).WithOne(o => o.Commission).HasForeignKey<MpCommission>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Provider).WithMany(p => p.Commissions).HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Payout).WithMany(p => p.Commissions).HasForeignKey(x => x.PayoutId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
            e.HasIndex(x => x.ProviderId).HasDatabaseName("ix_mp_commissions_provider");
            e.HasIndex(x => x.Status).HasDatabaseName("ix_mp_commissions_status");
        });

        modelBuilder.Entity<MpPayout>(e => {
            e.ToTable("mp_payouts");
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.Amount).HasColumnType("numeric(12,2)");
            e.Property(x => x.InitiatedAt).HasColumnType("timestamptz");
            e.Property(x => x.CompletedAt).HasColumnType("timestamptz");
            e.HasOne(x => x.Provider).WithMany(p => p.Payouts).HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ProviderId).HasDatabaseName("ix_mp_payouts_provider");
        });

        modelBuilder.Entity<MpReview>(e => {
            e.ToTable("mp_reviews");
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Series).WithMany(s => s.Reviews).HasForeignKey(x => x.SeriesId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(new[] { "StudentId", "SeriesId" }).IsUnique().HasDatabaseName("ix_mp_reviews_student_series");
        });

        modelBuilder.Entity<MpPromoCode>(e => {
            e.ToTable("mp_promo_codes");
            e.Property(x => x.DiscountType).HasConversion<int>();
            e.Property(x => x.DiscountValue).HasColumnType("numeric(12,2)");
            e.Property(x => x.MinOrderAmount).HasColumnType("numeric(12,2)");
            e.Property(x => x.MaxDiscount).HasColumnType("numeric(12,2)");
            e.Property(x => x.ValidFrom).HasColumnType("timestamptz");
            e.Property(x => x.ValidTo).HasColumnType("timestamptz");
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ix_mp_promo_code_unique");
        });

        modelBuilder.Entity<MpCmsBanner>(e => {
            e.ToTable("mp_cms_banners");
            e.Property(x => x.ValidFrom).HasColumnType("timestamptz");
            e.Property(x => x.ValidTo).HasColumnType("timestamptz");
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        });

        modelBuilder.Entity<MpOtpSession>(e => {
            e.ToTable("mp_otp_sessions");
            e.Property(x => x.ExpiresAt).HasColumnType("timestamptz");
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.HasIndex(x => x.Contact).HasDatabaseName("ix_mp_otp_contact");
        });

        // ── Exam Module ────────────────────────────────────────────────────────
        modelBuilder.Entity<ExamLevel>(e => {
            e.ToTable("exam_levels");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        });

        modelBuilder.Entity<ExamPage>(e => {
            e.ToTable("exam_pages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(300).IsRequired();
            e.Property(x => x.ShortDescription).HasMaxLength(500);
            e.Property(x => x.ConductingBody).HasMaxLength(300);
            e.Property(x => x.OfficialWebsite).HasMaxLength(500);
            e.Property(x => x.NotificationUrl).HasMaxLength(500);
            e.Property(x => x.ThumbnailUrl).HasMaxLength(500);
            e.Property(x => x.BannerUrl).HasMaxLength(500);
            e.Property(x => x.MetaTitle).HasMaxLength(300);
            e.Property(x => x.MetaDescription).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<int>().HasDefaultValue(ExamPageStatus.Draft);
            e.Property(x => x.IsFeatured).HasDefaultValue(false);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.ViewCount).HasDefaultValue(0);
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
            e.HasOne(x => x.ExamLevel).WithMany(l => l.ExamPages)
                .HasForeignKey(x => x.ExamLevelId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
            e.HasOne(x => x.ExamType).WithMany()
                .HasForeignKey(x => x.ExamTypeId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
            e.HasIndex(x => x.Slug).HasFilter("\"Status\" = 1").IsUnique()
                .HasDatabaseName("ix_exam_pages_slug_published");
            e.HasIndex(x => x.ExamLevelId).HasDatabaseName("ix_exam_pages_level");
            e.HasIndex(x => x.Status).HasDatabaseName("ix_exam_pages_status");
            e.HasIndex(x => x.IsFeatured).HasDatabaseName("ix_exam_pages_featured");
        });

        modelBuilder.Entity<ExamPageTest>(e => {
            e.ToTable("exam_page_tests");
            e.HasKey(x => new { x.ExamPageId, x.TestId });
            e.Property(x => x.IsFree).HasDefaultValue(true);
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.HasOne(x => x.ExamPage).WithMany(p => p.Tests)
                .HasForeignKey(x => x.ExamPageId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Test).WithMany()
                .HasForeignKey(x => x.TestId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ExamPageId).HasDatabaseName("ix_exam_page_tests_exam");
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Modified))
            if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;

        return base.SaveChangesAsync(cancellationToken);
    }
}

using GridAcademy.Data;
using GridAcademy.Data.Entities.Assessment;
using GridAcademy.Data.Entities.Content;
using GridAcademy.DTOs.Assessment;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services;

public class TestService : ITestService
{
    private readonly AppDbContext           _db;
    private readonly ILogger<TestService>   _logger;

    public TestService(AppDbContext db, ILogger<TestService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════════════════
    // TEST CRUD
    // ════════════════════════════════════════════════════════════════════════

    public async Task<List<TestListDto>> GetTestsAsync(TestListRequest request)
    {
        var query = _db.Tests
            .Include(t => t.ExamType)
            .Include(t => t.Sections)
            .AsNoTracking()
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);

        if (request.ExamTypeId.HasValue)
            query = query.Where(t => t.ExamTypeId == request.ExamTypeId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(t => t.Title.Contains(search));
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TestListDto
            {
                Id                    = t.Id,
                Title                 = t.Title,
                ExamTypeName          = t.ExamType.Name,
                Status                = t.Status,
                SectionCount          = t.Sections.Count,
                TotalQuestions        = t.Sections.Sum(s => s.QuestionCount),
                DurationMinutes       = t.DurationMinutes,
                PassingPercent        = t.PassingPercent,
                NegativeMarkingEnabled = t.NegativeMarkingEnabled,
                CreatedAt             = t.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<TestDetailDto> GetTestByIdAsync(Guid id)
    {
        var test = await _db.Tests
            .Include(t => t.ExamType)
            .Include(t => t.Sections)
                .ThenInclude(s => s.Subject)
            .Include(t => t.Sections)
                .ThenInclude(s => s.DifficultyLevel)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (test is null)
            throw new KeyNotFoundException($"Test {id} not found.");

        return MapToDetailDto(test);
    }

    public async Task<TestDetailDto> CreateTestAsync(CreateTestRequest request, Guid createdBy)
    {
        var test = new Test
        {
            Title                 = request.Title,
            Instructions          = request.Instructions,
            DurationMinutes       = request.DurationMinutes,
            PassingPercent        = request.PassingPercent,
            NegativeMarkingEnabled = request.NegativeMarkingEnabled,
            ExamTypeId            = request.ExamTypeId,
            Status                = TestStatus.Draft,
            CreatedAt             = DateTime.UtcNow,
            UpdatedAt             = DateTime.UtcNow,
            CreatedBy             = createdBy,
            UpdatedBy             = createdBy
        };

        _db.Tests.Add(test);
        await _db.SaveChangesAsync();

        return await GetTestByIdAsync(test.Id);
    }

    public async Task<TestDetailDto> UpdateTestAsync(Guid id, UpdateTestRequest request, Guid updatedBy)
    {
        var test = await _db.Tests.FindAsync(id)
            ?? throw new KeyNotFoundException($"Test {id} not found.");

        test.Title                 = request.Title;
        test.Instructions          = request.Instructions;
        test.DurationMinutes       = request.DurationMinutes;
        test.PassingPercent        = request.PassingPercent;
        test.NegativeMarkingEnabled = request.NegativeMarkingEnabled;
        test.ExamTypeId            = request.ExamTypeId;
        test.UpdatedBy             = updatedBy;
        // UpdatedAt set by AppDbContext.SaveChangesAsync interceptor

        await _db.SaveChangesAsync();

        return await GetTestByIdAsync(test.Id);
    }

    public async Task PublishTestAsync(Guid id)
    {
        var test = await _db.Tests
            .Include(t => t.Sections)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException($"Test {id} not found.");

        // Allow publishing if the test has directly-mapped questions even without sections.
        // Sections are only required when using the criteria-based pool mode.
        var hasDirectQuestions = await _db.TestQuestions.AnyAsync(tq => tq.TestId == id);

        if (!test.Sections.Any() && !hasDirectQuestions)
            throw new InvalidOperationException(
                "Cannot publish a test with no questions. Add questions via 'Add Question', " +
                "'Add from Bank', or 'Import', or set up Sections.");

        // When sections exist, validate each section has enough questions in its pool.
        if (test.Sections.Any())
        {
            var underFilled = new List<string>();
            foreach (var section in test.Sections)
            {
                var poolCount = await GetSectionPoolCountAsync(section.Id);
                if (poolCount < section.QuestionCount)
                    underFilled.Add(
                        $"Section \"{section.Name}\" needs {section.QuestionCount} question(s) " +
                        $"but only {poolCount} are available.");
            }

            if (underFilled.Any())
                throw new InvalidOperationException(
                    "Cannot publish — insufficient questions: " + string.Join("; ", underFilled) + ".");
        }

        test.Status = TestStatus.Published;
        await _db.SaveChangesAsync();
    }

    public async Task UnpublishTestAsync(Guid id)
    {
        var test = await _db.Tests.FindAsync(id)
            ?? throw new KeyNotFoundException($"Test {id} not found.");

        var hasActiveAttempts = await _db.TestAttempts
            .AnyAsync(a => a.TestId == id && a.Status == AttemptStatus.InProgress);

        if (hasActiveAttempts)
            throw new InvalidOperationException(
                "Cannot unpublish a test that has active (in-progress) attempts.");

        test.Status = TestStatus.Draft;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteTestAsync(Guid id)
    {
        var test = await _db.Tests.FindAsync(id)
            ?? throw new KeyNotFoundException($"Test {id} not found.");

        var hasAssignments = await _db.TestAssignments.AnyAsync(a => a.TestId == id);
        if (hasAssignments)
            throw new InvalidOperationException(
                "Cannot delete a test that has assignments. Revoke all assignments first.");

        _db.Tests.Remove(test);
        await _db.SaveChangesAsync();
    }

    // ════════════════════════════════════════════════════════════════════════
    // SECTIONS
    // ════════════════════════════════════════════════════════════════════════

    public async Task<TestSectionDto> AddSectionAsync(Guid testId, CreateTestSectionRequest request)
    {
        var testExists = await _db.Tests.AnyAsync(t => t.Id == testId);
        if (!testExists)
            throw new KeyNotFoundException($"Test {testId} not found.");

        var section = new TestSection
        {
            TestId                   = testId,
            Name                     = request.Name,
            SubjectId                = request.SubjectId,
            DifficultyLevelId        = request.DifficultyLevelId,
            QuestionCount            = request.QuestionCount,
            MarksPerQuestion         = request.MarksPerQuestion,
            NegativeMarksPerQuestion  = request.NegativeMarksPerQuestion,
            SortOrder                = request.SortOrder
        };

        _db.TestSections.Add(section);
        await _db.SaveChangesAsync();

        return await MapToSectionDtoAsync(section);
    }

    public async Task<TestSectionDto> UpdateSectionAsync(Guid testId, int sectionId, CreateTestSectionRequest request)
    {
        var section = await _db.TestSections
            .FirstOrDefaultAsync(s => s.Id == sectionId && s.TestId == testId)
            ?? throw new KeyNotFoundException($"Section {sectionId} not found on test {testId}.");

        section.Name                     = request.Name;
        section.SubjectId                = request.SubjectId;
        section.DifficultyLevelId        = request.DifficultyLevelId;
        section.QuestionCount            = request.QuestionCount;
        section.MarksPerQuestion         = request.MarksPerQuestion;
        section.NegativeMarksPerQuestion  = request.NegativeMarksPerQuestion;
        section.SortOrder                = request.SortOrder;

        await _db.SaveChangesAsync();

        return await MapToSectionDtoAsync(section);
    }

    public async Task DeleteSectionAsync(Guid testId, int sectionId)
    {
        var section = await _db.TestSections
            .FirstOrDefaultAsync(s => s.Id == sectionId && s.TestId == testId)
            ?? throw new KeyNotFoundException($"Section {sectionId} not found on test {testId}.");

        _db.TestSections.Remove(section);
        await _db.SaveChangesAsync();
    }

    // ════════════════════════════════════════════════════════════════════════
    // ASSIGNMENTS
    // ════════════════════════════════════════════════════════════════════════

    public async Task<List<TestAssignmentDto>> AssignTestAsync(
        Guid testId, AssignTestRequest request, Guid assignedBy)
    {
        var test = await _db.Tests.FindAsync(testId)
            ?? throw new KeyNotFoundException($"Test {testId} not found.");

        if (test.Status != TestStatus.Published)
            throw new InvalidOperationException("Only Published tests can be assigned.");

        // Load existing assignments for this test to avoid duplicates
        var existingStudentIds = (await _db.TestAssignments
            .Where(a => a.TestId == testId)
            .Select(a => a.StudentId)
            .ToListAsync()).ToHashSet();

        var created = new List<TestAssignment>();

        foreach (var studentId in request.StudentIds)
        {
            if (existingStudentIds.Contains(studentId))
            {
                _logger.LogWarning(
                    "Student {StudentId} already has an assignment for test {TestId}. Skipping.",
                    studentId, testId);
                continue;
            }

            var assignment = new TestAssignment
            {
                TestId        = testId,
                StudentId     = studentId,
                AvailableFrom = request.AvailableFrom,
                AvailableTo   = request.AvailableTo,
                MaxAttempts   = request.MaxAttempts,
                AssignedAt    = DateTime.UtcNow,
                AssignedBy    = assignedBy
            };

            _db.TestAssignments.Add(assignment);
            created.Add(assignment);
        }

        await _db.SaveChangesAsync();

        // Reload with student navigation for DTOs
        var createdIds = created.Select(a => a.Id).ToList();
        return await _db.TestAssignments
            .Include(a => a.Test)
            .Include(a => a.Student)
            .Include(a => a.Attempts)
            .AsNoTracking()
            .Where(a => createdIds.Contains(a.Id))
            .Select(a => MapToAssignmentDto(a, DateTime.UtcNow))
            .ToListAsync();
    }

    public async Task<List<TestAssignmentDto>> GetAssignmentsAsync(Guid testId)
    {
        var now = DateTime.UtcNow;
        return await _db.TestAssignments
            .Include(a => a.Test)
            .Include(a => a.Student)
            .Include(a => a.Attempts)
            .AsNoTracking()
            .Where(a => a.TestId == testId)
            .OrderBy(a => a.AssignedAt)
            .Select(a => MapToAssignmentDto(a, now))
            .ToListAsync();
    }

    public async Task<List<TestAssignmentDto>> GetStudentAssignmentsAsync(Guid studentId)
    {
        var now = DateTime.UtcNow;
        return await _db.TestAssignments
            .Include(a => a.Test)
            .Include(a => a.Student)
            .Include(a => a.Attempts)
            .AsNoTracking()
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.AvailableTo)
            .Select(a => MapToAssignmentDto(a, now))
            .ToListAsync();
    }

    public async Task RevokeAssignmentAsync(Guid assignmentId)
    {
        var assignment = await _db.TestAssignments.FindAsync(assignmentId)
            ?? throw new KeyNotFoundException($"Assignment {assignmentId} not found.");

        _db.TestAssignments.Remove(assignment);
        await _db.SaveChangesAsync();
    }

    // ════════════════════════════════════════════════════════════════════════
    // MANUAL QUESTION MANAGEMENT
    // ════════════════════════════════════════════════════════════════════════

    public async Task<List<TestQuestionDto>> GetTestQuestionsAsync(Guid testId)
    {
        return await _db.TestQuestions
            .Where(tq => tq.TestId == testId)
            .Include(tq => tq.Question).ThenInclude(q => q.Subject)
            .Include(tq => tq.Question).ThenInclude(q => q.DifficultyLevel)
            .Include(tq => tq.Section)
            .AsNoTracking()
            .OrderBy(tq => tq.SortOrder).ThenBy(tq => tq.AddedAt)
            .Select(tq => new TestQuestionDto
            {
                QuestionId      = tq.QuestionId,
                Text            = tq.Question.Text,
                SubjectName     = tq.Question.Subject != null ? tq.Question.Subject.Name : "",
                DifficultyLevel = tq.Question.DifficultyLevel != null ? tq.Question.DifficultyLevel.Name : "",
                QuestionType    = tq.Question.QuestionType.ToString(),
                SortOrder       = tq.SortOrder,
                SectionId       = tq.SectionId,
                SectionName     = tq.Section != null ? tq.Section.Name : ""
            })
            .ToListAsync();
    }

    public async Task AssignQuestionToSectionAsync(Guid testId, Guid questionId, int? sectionId)
    {
        var tq = await _db.TestQuestions
            .FirstOrDefaultAsync(x => x.TestId == testId && x.QuestionId == questionId)
            ?? throw new KeyNotFoundException("Question is not mapped to this test.");

        // Validate section belongs to this test if provided
        if (sectionId.HasValue)
        {
            var sectionExists = await _db.TestSections
                .AnyAsync(s => s.Id == sectionId.Value && s.TestId == testId);
            if (!sectionExists)
                throw new InvalidOperationException("Section does not belong to this test.");
        }

        tq.SectionId = sectionId;
        await _db.SaveChangesAsync();
    }

    public async Task<List<QuestionBrowseItem>> BrowseQuestionsForTestAsync(
        Guid testId, int? subjectId, int? difficultyLevelId, string? search)
    {
        var mappedIds = await _db.TestQuestions
            .Where(tq => tq.TestId == testId)
            .Select(tq => tq.QuestionId)
            .ToListAsync();
        var mappedSet = mappedIds.ToHashSet();

        var q = _db.Questions
            .Include(qe => qe.Subject)
            .Include(qe => qe.Topic)
            .Include(qe => qe.DifficultyLevel)
            .Where(qe => qe.Status == Data.Entities.Content.QuestionStatus.Published)
            .AsNoTracking()
            .AsQueryable();

        if (subjectId.HasValue)
            q = q.Where(qe => qe.SubjectId == subjectId.Value);

        if (difficultyLevelId.HasValue)
            q = q.Where(qe => qe.DifficultyLevelId == difficultyLevelId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(qe => qe.Text.ToLower().Contains(s));
        }

        var items = await q
            .OrderBy(qe => qe.Subject != null ? qe.Subject.Name : "")
            .ThenByDescending(qe => qe.CreatedAt)
            .Take(60)
            .ToListAsync();

        return items.Select(qe => new QuestionBrowseItem
        {
            Id              = qe.Id,
            Text            = qe.Text,
            SubjectName     = qe.Subject?.Name ?? "",
            TopicName       = qe.Topic?.Name ?? "",
            DifficultyLevel = qe.DifficultyLevel?.Name ?? "",
            QuestionType    = qe.QuestionType.ToString(),
            AlreadyMapped   = mappedSet.Contains(qe.Id)
        }).ToList();
    }

    public async Task AddQuestionsToTestAsync(Guid testId, List<Guid> questionIds)
    {
        if (questionIds is null || questionIds.Count == 0) return;

        var existing = (await _db.TestQuestions
            .Where(tq => tq.TestId == testId)
            .Select(tq => tq.QuestionId)
            .ToListAsync()).ToHashSet();

        var maxSort = await _db.TestQuestions
            .Where(tq => tq.TestId == testId)
            .MaxAsync(tq => (int?)tq.SortOrder) ?? 0;

        foreach (var qid in questionIds.Distinct().Where(q => !existing.Contains(q)))
        {
            maxSort++;
            _db.TestQuestions.Add(new TestQuestion
            {
                TestId     = testId,
                QuestionId = qid,
                SortOrder  = maxSort,
                AddedAt    = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task RemoveQuestionFromTestAsync(Guid testId, Guid questionId)
    {
        var tq = await _db.TestQuestions
            .FirstOrDefaultAsync(tq => tq.TestId == testId && tq.QuestionId == questionId);

        if (tq is not null)
        {
            _db.TestQuestions.Remove(tq);
            await _db.SaveChangesAsync();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // POOL VALIDATION
    // ════════════════════════════════════════════════════════════════════════

    public async Task<int> GetSectionPoolCountAsync(int sectionId)
    {
        var section = await _db.TestSections
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sectionId)
            ?? throw new KeyNotFoundException($"Section {sectionId} not found.");

        // Mirror the runtime logic in AssessmentService.GetCandidateIdsAsync:
        // 1. Direct mappings via TestQuestion take precedence (questions imported/mapped to this test).
        // 2. Fall back to criteria-based pool (subject + optional difficulty filter).
        var directCount = await _db.TestQuestions
            .Where(tq => tq.TestId == section.TestId)
            .Join(_db.Questions.Where(q => q.SubjectId == section.SubjectId),
                tq => tq.QuestionId, q => q.Id, (tq, q) => q.Id)
            .CountAsync();

        if (directCount > 0)
            return directCount;

        // Criteria-based pool (all questions matching subject/difficulty).
        var query = _db.Questions
            .Where(q => q.SubjectId == section.SubjectId);

        if (section.DifficultyLevelId.HasValue)
            query = query.Where(q => q.DifficultyLevelId == section.DifficultyLevelId.Value);

        return await query.CountAsync();
    }

    // ════════════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ════════════════════════════════════════════════════════════════════════

    private static TestDetailDto MapToDetailDto(Test test)
    {
        return new TestDetailDto
        {
            Id                     = test.Id,
            Title                  = test.Title,
            Instructions           = test.Instructions,
            DurationMinutes        = test.DurationMinutes,
            PassingPercent         = test.PassingPercent,
            NegativeMarkingEnabled  = test.NegativeMarkingEnabled,
            ExamTypeId             = test.ExamTypeId,
            ExamTypeName           = test.ExamType?.Name ?? "",
            Status                 = test.Status,
            CreatedAt              = test.CreatedAt,
            UpdatedAt              = test.UpdatedAt,
            Sections               = test.Sections
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Id)
                .Select(s => new TestSectionDto
                {
                    Id                       = s.Id,
                    TestId                   = s.TestId,
                    Name                     = s.Name,
                    SubjectId                = s.SubjectId,
                    SubjectName              = s.Subject?.Name ?? "",
                    DifficultyLevelId        = s.DifficultyLevelId,
                    DifficultyLevelName      = s.DifficultyLevel?.Name,
                    QuestionCount            = s.QuestionCount,
                    MarksPerQuestion         = s.MarksPerQuestion,
                    NegativeMarksPerQuestion  = s.NegativeMarksPerQuestion,
                    SortOrder                = s.SortOrder,
                    AvailableInPool          = 0  // not pre-populated on detail view; use GetSectionPoolCountAsync
                })
                .ToList()
        };
    }

    private async Task<TestSectionDto> MapToSectionDtoAsync(TestSection section)
    {
        // Reload with navigations
        var loaded = await _db.TestSections
            .Include(s => s.Subject)
            .Include(s => s.DifficultyLevel)
            .AsNoTracking()
            .FirstAsync(s => s.Id == section.Id);

        var poolCount = await GetSectionPoolCountAsync(loaded.Id);

        return new TestSectionDto
        {
            Id                       = loaded.Id,
            TestId                   = loaded.TestId,
            Name                     = loaded.Name,
            SubjectId                = loaded.SubjectId,
            SubjectName              = loaded.Subject?.Name ?? "",
            DifficultyLevelId        = loaded.DifficultyLevelId,
            DifficultyLevelName      = loaded.DifficultyLevel?.Name,
            QuestionCount            = loaded.QuestionCount,
            MarksPerQuestion         = loaded.MarksPerQuestion,
            NegativeMarksPerQuestion  = loaded.NegativeMarksPerQuestion,
            SortOrder                = loaded.SortOrder,
            AvailableInPool          = poolCount
        };
    }

    private static TestAssignmentDto MapToAssignmentDto(TestAssignment a, DateTime now)
    {
        return new TestAssignmentDto
        {
            Id             = a.Id,
            TestId         = a.TestId,
            TestTitle      = a.Test?.Title ?? "",
            StudentId      = a.StudentId,
            StudentName    = a.Student is null ? "" : $"{a.Student.FirstName} {a.Student.LastName}".Trim(),
            StudentEmail   = a.Student?.Email ?? "",
            AvailableFrom  = a.AvailableFrom,
            AvailableTo    = a.AvailableTo,
            MaxAttempts    = a.MaxAttempts,
            AttemptsUsed   = a.Attempts?.Count ?? 0,
            IsActive       = a.AvailableFrom <= now && now <= a.AvailableTo
        };
    }
}

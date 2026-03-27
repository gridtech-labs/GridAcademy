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

        if (!test.Sections.Any())
            throw new InvalidOperationException("Cannot publish a test with no sections.");

        // Validate each section has enough questions in the pool (Draft + Published).
        // The Published-only constraint is enforced at exam-start time.
        var underFilled = new List<string>();
        foreach (var section in test.Sections)
        {
            var poolCount = await GetSectionPoolCountAsync(section.Id);
            if (poolCount < section.QuestionCount)
                underFilled.Add(
                    $"'{section.Name}' needs {section.QuestionCount} question(s) " +
                    $"but only {poolCount} exist for the selected Subject/Difficulty combination");
        }

        if (underFilled.Any())
            throw new InvalidOperationException(
                "Not enough questions in the pool — " + string.Join("; ", underFilled) +
                ". Add more questions or reduce the section count.");

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
    // POOL VALIDATION
    // ════════════════════════════════════════════════════════════════════════

    public async Task<int> GetSectionPoolCountAsync(int sectionId)
    {
        var section = await _db.TestSections
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sectionId)
            ?? throw new KeyNotFoundException($"Section {sectionId} not found.");

        // Count ALL questions matching subject/difficulty regardless of Draft/Published status.
        // The Published-only filter is enforced at exam-start time (AssessmentService.BuildCandidateQuery).
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

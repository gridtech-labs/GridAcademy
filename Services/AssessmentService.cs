using System.Text.Json;
using GridAcademy.Data;
using GridAcademy.Data.Entities.Assessment;
using GridAcademy.Data.Entities.Content;
using GridAcademy.DTOs.Assessment;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services;

public class AssessmentService : IAssessmentService
{
    private readonly AppDbContext                _db;
    private readonly ILogger<AssessmentService>  _logger;

    public AssessmentService(AppDbContext db, ILogger<AssessmentService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════════════════
    // STUDENT-FACING
    // ════════════════════════════════════════════════════════════════════════

    public async Task<List<StudentTestCardDto>> GetAvailableTestsAsync(Guid studentId)
    {
        var now = DateTime.UtcNow;

        var assignments = await _db.TestAssignments
            .Include(a => a.Test)
                .ThenInclude(t => t.ExamType)
            .Include(a => a.Test)
                .ThenInclude(t => t.Sections)
                    .ThenInclude(s => s.Subject)
            .Include(a => a.Test)
                .ThenInclude(t => t.Sections)
                    .ThenInclude(s => s.DifficultyLevel)
            .Include(a => a.Attempts)
            .AsNoTracking()
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.AvailableTo)
            .ToListAsync();

        var result = new List<StudentTestCardDto>();

        foreach (var assignment in assignments)
        {
            var attempts          = assignment.Attempts.OrderByDescending(a => a.StartedAt).ToList();
            var completedAttempts = attempts.Where(a => a.Status is AttemptStatus.Submitted or AttemptStatus.TimedOut).ToList();
            var inProgressAttempt = attempts.FirstOrDefault(a => a.Status == AttemptStatus.InProgress);
            var lastCompleted     = completedAttempts.FirstOrDefault();

            var card = new StudentTestCardDto
            {
                AssignmentId          = assignment.Id,
                TestId                = assignment.TestId,
                Title                 = assignment.Test.Title,
                ExamTypeName          = assignment.Test.ExamType?.Name ?? "",
                DurationMinutes       = assignment.Test.DurationMinutes,
                TotalQuestions        = assignment.Test.Sections.Sum(s => s.QuestionCount),
                SectionCount          = assignment.Test.Sections.Count,
                PassingPercent        = assignment.Test.PassingPercent,
                NegativeMarkingEnabled = assignment.Test.NegativeMarkingEnabled,
                AvailableFrom         = assignment.AvailableFrom,
                AvailableTo           = assignment.AvailableTo,
                MaxAttempts           = assignment.MaxAttempts,
                AttemptsUsed          = completedAttempts.Count,
                AttemptsRemaining     = Math.Max(0, assignment.MaxAttempts - completedAttempts.Count),
                HasInProgressAttempt  = inProgressAttempt is not null,
                InProgressAttemptId   = inProgressAttempt?.Id,
                LastCompletedAttemptId = lastCompleted?.Id,
                Sections              = assignment.Test.Sections
                    .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
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
                        AvailableInPool          = 0
                    })
                    .ToList()
            };

            result.Add(card);
        }

        return result;
    }

    public async Task<AttemptStartDto> StartAttemptAsync(Guid assignmentId, Guid studentId)
    {
        var assignment = await _db.TestAssignments
            .Include(a => a.Test)
                .ThenInclude(t => t.Sections)
            .Include(a => a.Attempts)
            .FirstOrDefaultAsync(a => a.Id == assignmentId)
            ?? throw new KeyNotFoundException($"Assignment {assignmentId} not found.");

        if (assignment.StudentId != studentId)
            throw new UnauthorizedAccessException("This assignment does not belong to the requesting student.");

        var now = DateTime.UtcNow;
        if (now < assignment.AvailableFrom || now > assignment.AvailableTo)
            throw new InvalidOperationException(
                $"This test is only available from {assignment.AvailableFrom:u} to {assignment.AvailableTo:u}.");

        var completedAttempts = assignment.Attempts
            .Where(a => a.Status is AttemptStatus.Submitted or AttemptStatus.TimedOut)
            .ToList();

        if (completedAttempts.Count >= assignment.MaxAttempts)
            throw new InvalidOperationException(
                $"Maximum attempt limit of {assignment.MaxAttempts} has been reached.");

        var existingInProgress = assignment.Attempts
            .FirstOrDefault(a => a.Status == AttemptStatus.InProgress);

        if (existingInProgress is not null)
            throw new InvalidOperationException(
                $"An in-progress attempt already exists (attempt ID: {existingInProgress.Id}). " +
                "Please resume your existing attempt.");

        // Create the attempt
        var attempt = new TestAttempt
        {
            AssignmentId  = assignmentId,
            StudentId     = studentId,
            TestId        = assignment.TestId,
            AttemptNumber = completedAttempts.Count + 1,
            Status        = AttemptStatus.InProgress,
            StartedAt     = now
        };

        _db.TestAttempts.Add(attempt);

        // Select questions per section
        var sections = assignment.Test.Sections
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToList();

        int globalDisplayOrder = 1;

        for (int sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
        {
            var section = sections[sectionIndex];

            // Load candidate question IDs from DB in a single query
            var candidateIds = await BuildCandidateQuery(section)
                .Select(q => q.Id)
                .ToListAsync();

            if (candidateIds.Count < section.QuestionCount)
            {
                _logger.LogWarning(
                    "Section '{SectionName}' requested {Needed} questions but pool only has {Pool}.",
                    section.Name, section.QuestionCount, candidateIds.Count);
            }

            // Shuffle in memory and take the required count
            var selected = candidateIds
                .OrderBy(_ => Guid.NewGuid())
                .Take(section.QuestionCount)
                .ToList();

            for (int posInSection = 0; posInSection < selected.Count; posInSection++)
            {
                _db.AttemptQuestions.Add(new AttemptQuestion
                {
                    Attempt               = attempt,
                    QuestionId            = selected[posInSection],
                    SectionIndex          = sectionIndex,
                    SectionName           = section.Name,
                    DisplayOrder          = globalDisplayOrder++,
                    DisplayOrderInSection = posInSection + 1,
                    MarksForCorrect       = section.MarksPerQuestion,
                    NegativeMarks         = section.NegativeMarksPerQuestion,
                    IsVisited             = false,
                    IsMarkedForReview     = false
                });
            }
        }

        await _db.SaveChangesAsync();

        // Reload with Question navigations for DTO projection
        return await BuildAttemptStartDtoAsync(attempt.Id, assignment.Test);
    }

    public async Task<AttemptStateDto> GetAttemptStateAsync(Guid attemptId, Guid studentId)
    {
        var attempt = await _db.TestAttempts
            .Include(a => a.Assignment)
                .ThenInclude(a => a.Test)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attemptId)
            ?? throw new KeyNotFoundException($"Attempt {attemptId} not found.");

        if (attempt.StudentId != studentId)
            throw new UnauthorizedAccessException("This attempt does not belong to the requesting student.");

        var startDto = await BuildAttemptStartDtoAsync(attemptId, attempt.Assignment.Test);

        return new AttemptStateDto
        {
            AttemptId              = startDto.AttemptId,
            TestTitle              = startDto.TestTitle,
            Instructions           = startDto.Instructions,
            DurationSeconds        = startDto.DurationSeconds,
            SecondsElapsed         = startDto.SecondsElapsed,
            TotalQuestions         = startDto.TotalQuestions,
            NegativeMarkingEnabled = startDto.NegativeMarkingEnabled,
            Questions              = startDto.Questions,
            SavedAnswers           = startDto.SavedAnswers,
            Status                 = attempt.Status
        };
    }

    public async Task SaveAnswerAsync(Guid attemptId, SaveAnswerRequest request, Guid studentId)
    {
        var attempt = await _db.TestAttempts
            .Include(a => a.Assignment)
                .ThenInclude(a => a.Test)
            .FirstOrDefaultAsync(a => a.Id == attemptId)
            ?? throw new KeyNotFoundException($"Attempt {attemptId} not found.");

        if (attempt.StudentId != studentId)
            throw new UnauthorizedAccessException("This attempt does not belong to the requesting student.");

        if (attempt.Status != AttemptStatus.InProgress)
            throw new InvalidOperationException("Answers can only be saved for an in-progress attempt.");

        // Check time limit
        var durationSeconds = attempt.Assignment.Test.DurationMinutes * 60;
        var elapsed         = (DateTime.UtcNow - attempt.StartedAt).TotalSeconds;
        if (elapsed > durationSeconds)
        {
            // Time expired — auto-submit before throwing
            _logger.LogInformation("Attempt {AttemptId} timed out. Auto-submitting.", attemptId);
            await SubmitAttemptAsync(attemptId, studentId);
            throw new InvalidOperationException("Time expired. The attempt has been submitted automatically.");
        }

        // Upsert AttemptAnswer
        var existingAnswer = await _db.AttemptAnswers
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.QuestionId == request.QuestionId);

        var selectedOptionString = request.SelectedOptionIds.Count > 0
            ? string.Join(",", request.SelectedOptionIds)
            : null;

        if (existingAnswer is null)
        {
            _db.AttemptAnswers.Add(new AttemptAnswer
            {
                AttemptId         = attemptId,
                QuestionId        = request.QuestionId,
                SelectedOptionIds = selectedOptionString,
                NumericalValue    = request.NumericalValue,
                IsClear           = request.IsClear,
                SavedAt           = DateTime.UtcNow
            });
        }
        else
        {
            existingAnswer.SelectedOptionIds = selectedOptionString;
            existingAnswer.NumericalValue    = request.NumericalValue;
            existingAnswer.IsClear           = request.IsClear;
            existingAnswer.SavedAt           = DateTime.UtcNow;
        }

        // Mark question as visited
        var attemptQuestion = await _db.AttemptQuestions
            .FirstOrDefaultAsync(q => q.AttemptId == attemptId && q.QuestionId == request.QuestionId);

        if (attemptQuestion is not null && !attemptQuestion.IsVisited)
            attemptQuestion.IsVisited = true;

        await _db.SaveChangesAsync();
    }

    public async Task ToggleMarkForReviewAsync(Guid attemptId, Guid questionId, Guid studentId)
    {
        var attempt = await _db.TestAttempts.FindAsync(attemptId)
            ?? throw new KeyNotFoundException($"Attempt {attemptId} not found.");

        if (attempt.StudentId != studentId)
            throw new UnauthorizedAccessException("This attempt does not belong to the requesting student.");

        var aq = await _db.AttemptQuestions
            .FirstOrDefaultAsync(q => q.AttemptId == attemptId && q.QuestionId == questionId)
            ?? throw new KeyNotFoundException($"Question {questionId} not found in attempt {attemptId}.");

        aq.IsMarkedForReview = !aq.IsMarkedForReview;
        await _db.SaveChangesAsync();
    }

    public async Task MarkVisitedAsync(Guid attemptId, Guid questionId, Guid studentId)
    {
        var attempt = await _db.TestAttempts.FindAsync(attemptId)
            ?? throw new KeyNotFoundException($"Attempt {attemptId} not found.");

        if (attempt.StudentId != studentId)
            throw new UnauthorizedAccessException("This attempt does not belong to the requesting student.");

        var aq = await _db.AttemptQuestions
            .FirstOrDefaultAsync(q => q.AttemptId == attemptId && q.QuestionId == questionId)
            ?? throw new KeyNotFoundException($"Question {questionId} not found in attempt {attemptId}.");

        if (!aq.IsVisited)
        {
            aq.IsVisited = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task LogViolationAsync(Guid attemptId, string violationType, Guid studentId)
    {
        var attempt = await _db.TestAttempts
            .FirstOrDefaultAsync(a => a.Id == attemptId)
            ?? throw new KeyNotFoundException($"Attempt {attemptId} not found.");

        if (attempt.StudentId != studentId)
            throw new UnauthorizedAccessException("This attempt does not belong to the requesting student.");

        // Parse existing log or start fresh
        var violations = new List<JsonElement>();
        if (!string.IsNullOrWhiteSpace(attempt.ViolationLog))
        {
            try
            {
                var existing = JsonSerializer.Deserialize<List<JsonElement>>(attempt.ViolationLog);
                if (existing is not null)
                    violations.AddRange(existing);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Could not parse existing ViolationLog for attempt {AttemptId}.", attemptId);
            }
        }

        // Build new violation entry as a raw document
        var newEntry = new
        {
            type      = violationType,
            timestamp = DateTime.UtcNow.ToString("O")
        };

        var serialised = JsonSerializer.Serialize(newEntry);
        using var doc  = JsonDocument.Parse(serialised);
        violations.Add(doc.RootElement.Clone());

        attempt.ViolationLog = JsonSerializer.Serialize(violations);
        await _db.SaveChangesAsync();
    }

    public async Task<AttemptResultDto> SubmitAttemptAsync(Guid attemptId, Guid studentId)
    {
        var attempt = await _db.TestAttempts
            .Include(a => a.Questions)
                .ThenInclude(q => q.Question)
                    .ThenInclude(q => q.Options)
            .Include(a => a.Answers)
            .Include(a => a.Assignment)
                .ThenInclude(a => a.Test)
            .FirstOrDefaultAsync(a => a.Id == attemptId)
            ?? throw new KeyNotFoundException($"Attempt {attemptId} not found.");

        if (attempt.StudentId != studentId)
            throw new UnauthorizedAccessException("This attempt does not belong to the requesting student.");

        if (attempt.Status is AttemptStatus.Submitted or AttemptStatus.TimedOut)
        {
            // Already submitted — just return the existing result
            return await GetResultAsync(attemptId, studentId);
        }

        if (attempt.Status != AttemptStatus.InProgress)
            throw new InvalidOperationException($"Attempt is not in progress (status: {attempt.Status}).");

        var test = attempt.Assignment.Test;

        // Build answer lookup
        var answerByQuestionId = attempt.Answers
            .ToDictionary(a => a.QuestionId);

        // Group questions by section
        var bySection = attempt.Questions
            .GroupBy(q => q.SectionIndex)
            .OrderBy(g => g.Key)
            .ToList();

        decimal totalMarksObtained  = 0m;
        decimal totalMarksPossible  = 0m;
        var     sectionResults      = new List<AttemptSectionResult>();

        foreach (var sectionGroup in bySection)
        {
            var sectionIndex    = sectionGroup.Key;
            var sectionName     = sectionGroup.First().SectionName;
            var sectionQuestions = sectionGroup.OrderBy(q => q.DisplayOrderInSection).ToList();

            decimal sectionMarksObtained = 0m;
            decimal sectionMaxMarks      = 0m;
            int     correct              = 0;
            int     incorrect            = 0;
            int     unattempted          = 0;
            int     attempted            = 0;

            foreach (var aq in sectionQuestions)
            {
                var q      = aq.Question;
                sectionMaxMarks += aq.MarksForCorrect;
                totalMarksPossible += aq.MarksForCorrect;

                answerByQuestionId.TryGetValue(aq.QuestionId, out var answer);

                decimal marksForThis = 0m;
                bool    isCorrect    = false;   // tracked for future use (e.g., result DTO)
                bool    isAttempted  = answer is not null && !answer.IsClear;

                if (q.QuestionType == QuestionType.MCQ || q.QuestionType == QuestionType.TrueFalse
                    || q.QuestionType == QuestionType.AssertionReason)
                {
                    // Single-correct: exactly one correct option
                    if (!isAttempted)
                    {
                        unattempted++;
                    }
                    else
                    {
                        attempted++;
                        var correctOptionId = q.Options
                            .Where(o => o.IsCorrect)
                            .Select(o => o.Id)
                            .FirstOrDefault();

                        var studentOptionId = ParseOptionIds(answer!.SelectedOptionIds)
                            .FirstOrDefault();

                        if (correctOptionId != 0 && studentOptionId == correctOptionId)
                        {
                            isCorrect     = true;
                            marksForThis  = aq.MarksForCorrect;
                            correct++;
                        }
                        else
                        {
                            incorrect++;
                            if (test.NegativeMarkingEnabled)
                                marksForThis = -aq.NegativeMarks;
                        }
                    }
                }
                else if (q.QuestionType == QuestionType.MSQ)
                {
                    // Multi-correct: all-or-nothing
                    if (!isAttempted)
                    {
                        unattempted++;
                    }
                    else
                    {
                        attempted++;
                        var correctIds = q.Options
                            .Where(o => o.IsCorrect)
                            .Select(o => o.Id)
                            .OrderBy(id => id)
                            .ToList();

                        var studentIds = ParseOptionIds(answer!.SelectedOptionIds)
                            .OrderBy(id => id)
                            .ToList();

                        if (correctIds.SequenceEqual(studentIds))
                        {
                            isCorrect    = true;
                            marksForThis = aq.MarksForCorrect;
                            correct++;
                        }
                        else
                        {
                            incorrect++;
                            if (test.NegativeMarkingEnabled)
                                marksForThis = -aq.NegativeMarks;
                        }
                    }
                }
                else if (q.QuestionType == QuestionType.NAT)
                {
                    // Numerical: no negative marking
                    if (!isAttempted || answer!.NumericalValue is null)
                    {
                        unattempted++;
                    }
                    else
                    {
                        attempted++;
                        var tolerance    = q.NumericalTolerance ?? 0m;
                        var studentValue = answer.NumericalValue!.Value;
                        var correct_val  = q.NumericalAnswer ?? 0m;

                        if (Math.Abs(studentValue - correct_val) <= tolerance)
                        {
                            isCorrect    = true;
                            marksForThis = aq.MarksForCorrect;
                            correct++;
                        }
                        else
                        {
                            incorrect++;
                            // NAT: no negative marking
                        }
                    }
                }
                else
                {
                    // Other question types (FillInBlanks, MatchTheFollowing, etc.)
                    // treated as unattempted / zero score for now
                    unattempted++;
                }

                sectionMarksObtained += marksForThis;
                totalMarksObtained   += marksForThis;
            }

            sectionResults.Add(new AttemptSectionResult
            {
                AttemptId      = attemptId,
                SectionIndex   = sectionIndex,
                SectionName    = sectionName,
                TotalQuestions = sectionQuestions.Count,
                Attempted      = attempted,
                Correct        = correct,
                Incorrect      = incorrect,
                Unattempted    = unattempted,
                MarksObtained  = sectionMarksObtained,
                MaxMarks       = sectionMaxMarks
            });
        }

        // Finalise attempt
        var submittedAt       = DateTime.UtcNow;
        var secondsUsed       = (int)(submittedAt - attempt.StartedAt).TotalSeconds;
        var percentage        = totalMarksPossible > 0
            ? Math.Round(totalMarksObtained / totalMarksPossible * 100m, 2)
            : 0m;

        attempt.TotalMarksObtained   = totalMarksObtained;
        attempt.TotalMarksPossible   = totalMarksPossible;
        attempt.Percentage           = percentage;
        attempt.IsPassed             = percentage >= test.PassingPercent;
        attempt.Status               = AttemptStatus.Submitted;
        attempt.SubmittedAt          = submittedAt;
        attempt.DurationSecondsUsed  = secondsUsed;

        _db.AttemptSectionResults.AddRange(sectionResults);

        await _db.SaveChangesAsync();

        return await GetResultAsync(attemptId, studentId);
    }

    public async Task<AttemptResultDto> GetResultAsync(Guid attemptId, Guid studentId)
    {
        return await BuildResultDtoAsync(attemptId, studentId, adminMode: false);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ADMIN
    // ════════════════════════════════════════════════════════════════════════

    public async Task<List<AttemptSummaryDto>> GetAttemptsByTestAsync(Guid testId)
    {
        return await _db.TestAttempts
            .Include(a => a.Assignment)
                .ThenInclude(a => a.Student)
            .AsNoTracking()
            .Where(a => a.TestId == testId)
            .OrderByDescending(a => a.StartedAt)
            .Select(a => new AttemptSummaryDto
            {
                AttemptId      = a.Id,
                StudentId      = a.StudentId,
                StudentName    = a.Assignment.Student.FirstName + " " + a.Assignment.Student.LastName,
                StudentEmail   = a.Assignment.Student.Email,
                AttemptNumber  = a.AttemptNumber,
                Status         = a.Status,
                StartedAt      = a.StartedAt,
                SubmittedAt    = a.SubmittedAt,
                Percentage     = a.Percentage,
                IsPassed       = a.IsPassed,
                ViolationCount = a.ViolationLog == null ? 0
                    : CountViolations(a.ViolationLog)
            })
            .ToListAsync();
    }

    public async Task<AttemptResultDto> GetAttemptDetailAsync(Guid attemptId)
    {
        // Admin path: no student ownership check
        return await BuildResultDtoAsync(attemptId, studentId: Guid.Empty, adminMode: true);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds the AttemptStartDto (used by StartAttemptAsync and GetAttemptStateAsync).
    /// Always reloads attempt questions from the DB so navigations are populated.
    /// </summary>
    private async Task<AttemptStartDto> BuildAttemptStartDtoAsync(Guid attemptId, Test test)
    {
        var attempt = await _db.TestAttempts
            .Include(a => a.Questions)
                .ThenInclude(q => q.Question)
                    .ThenInclude(q => q.Options)
            .Include(a => a.Answers)
            .AsNoTracking()
            .FirstAsync(a => a.Id == attemptId);

        var durationSeconds = test.DurationMinutes * 60;
        var elapsed         = (int)Math.Min(
            (DateTime.UtcNow - attempt.StartedAt).TotalSeconds,
            durationSeconds);

        var questions = attempt.Questions
            .OrderBy(q => q.DisplayOrder)
            .Select(aq => new AttemptQuestionDto
            {
                AttemptQuestionId    = aq.Id,
                QuestionId           = aq.QuestionId,
                SectionIndex         = aq.SectionIndex,
                SectionName          = aq.SectionName,
                DisplayOrder         = aq.DisplayOrder,
                DisplayOrderInSection = aq.DisplayOrderInSection,
                Text                 = aq.Question.Text,
                QuestionType         = aq.Question.QuestionType,
                Options              = aq.Question.Options
                    .OrderBy(o => o.SortOrder).ThenBy(o => o.Label)
                    .Select(o => new AttemptOptionDto
                    {
                        Id    = o.Id,
                        Label = o.Label,
                        Text  = o.Text
                        // IsCorrect intentionally excluded
                    })
                    .ToList(),
                IsVisited            = aq.IsVisited,
                IsMarkedForReview    = aq.IsMarkedForReview,
                MarksForCorrect      = aq.MarksForCorrect,
                NegativeMarks        = aq.NegativeMarks
            })
            .ToList();

        var answerByQuestionId = attempt.Answers.ToDictionary(a => a.QuestionId);
        var savedAnswers = attempt.Questions
            .OrderBy(q => q.DisplayOrder)
            .Select(aq =>
            {
                answerByQuestionId.TryGetValue(aq.QuestionId, out var ans);
                return new SavedAnswerDto
                {
                    QuestionId          = aq.QuestionId,
                    SelectedOptionIds   = ans is not null
                        ? ParseOptionIds(ans.SelectedOptionIds)
                        : [],
                    NumericalValue      = ans?.NumericalValue,
                    IsClear             = ans?.IsClear ?? false,
                    IsMarkedForReview   = aq.IsMarkedForReview,
                    IsVisited           = aq.IsVisited
                };
            })
            .ToList();

        return new AttemptStartDto
        {
            AttemptId              = attempt.Id,
            TestTitle              = test.Title,
            Instructions           = test.Instructions,
            DurationSeconds        = durationSeconds,
            SecondsElapsed         = elapsed,
            TotalQuestions         = questions.Count,
            NegativeMarkingEnabled = test.NegativeMarkingEnabled,
            Questions              = questions,
            SavedAnswers           = savedAnswers
        };
    }

    /// <summary>
    /// Full result builder shared by GetResultAsync (student) and GetAttemptDetailAsync (admin).
    /// When adminMode=false the attempt's StudentId must match studentId.
    /// </summary>
    private async Task<AttemptResultDto> BuildResultDtoAsync(Guid attemptId, Guid studentId, bool adminMode)
    {
        var attempt = await _db.TestAttempts
            .Include(a => a.Questions)
                .ThenInclude(q => q.Question)
                    .ThenInclude(q => q.Options)
            .Include(a => a.Answers)
            .Include(a => a.SectionResults)
            .Include(a => a.Assignment)
                .ThenInclude(a => a.Test)
            .Include(a => a.Assignment)
                .ThenInclude(a => a.Student)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attemptId)
            ?? throw new KeyNotFoundException($"Attempt {attemptId} not found.");

        if (!adminMode && attempt.StudentId != studentId)
            throw new UnauthorizedAccessException("This attempt does not belong to the requesting student.");

        // Guard: result is only available for submitted/timed-out attempts
        if (!adminMode && attempt.Status == AttemptStatus.InProgress)
            throw new InvalidOperationException("This attempt is still in progress. Please complete and submit the test first.");

        var test    = attempt.Assignment.Test;
        var student = attempt.Assignment.Student;

        var answerByQuestionId = attempt.Answers.ToDictionary(a => a.QuestionId);

        // ── Rebuild per-question scores (needed in result view) ──────────────
        var questionResults = attempt.Questions
            .OrderBy(q => q.DisplayOrder)
            .Select(aq =>
            {
                var q = aq.Question;
                answerByQuestionId.TryGetValue(aq.QuestionId, out var answer);

                bool    isAttempted      = answer is not null && !answer.IsClear;
                bool    isCorrect        = false;
                decimal marksAwarded     = 0m;
                var     studentOptionIds = ParseOptionIds(answer?.SelectedOptionIds);
                var     correctOptionIds = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList();

                if (q.QuestionType is QuestionType.MCQ or QuestionType.TrueFalse or QuestionType.AssertionReason)
                {
                    if (isAttempted)
                    {
                        var studentOpt  = studentOptionIds.FirstOrDefault();
                        var correctOpt  = correctOptionIds.FirstOrDefault();
                        if (correctOpt != 0 && studentOpt == correctOpt)
                        {
                            isCorrect    = true;
                            marksAwarded = aq.MarksForCorrect;
                        }
                        else if (test.NegativeMarkingEnabled)
                        {
                            marksAwarded = -aq.NegativeMarks;
                        }
                    }
                }
                else if (q.QuestionType == QuestionType.MSQ)
                {
                    if (isAttempted)
                    {
                        var correctSorted = correctOptionIds.OrderBy(id => id).ToList();
                        var studentSorted = studentOptionIds.OrderBy(id => id).ToList();
                        if (correctSorted.SequenceEqual(studentSorted))
                        {
                            isCorrect    = true;
                            marksAwarded = aq.MarksForCorrect;
                        }
                        else if (test.NegativeMarkingEnabled)
                        {
                            marksAwarded = -aq.NegativeMarks;
                        }
                    }
                }
                else if (q.QuestionType == QuestionType.NAT)
                {
                    if (isAttempted && answer!.NumericalValue.HasValue)
                    {
                        var tolerance   = q.NumericalTolerance ?? 0m;
                        var correctVal  = q.NumericalAnswer ?? 0m;
                        if (Math.Abs(answer.NumericalValue.Value - correctVal) <= tolerance)
                        {
                            isCorrect    = true;
                            marksAwarded = aq.MarksForCorrect;
                        }
                    }
                }

                return new QuestionResultDto
                {
                    DisplayOrder           = aq.DisplayOrder,
                    SectionName            = aq.SectionName,
                    QuestionText           = q.Text,
                    QuestionType           = q.QuestionType,
                    Options                = q.Options
                        .OrderBy(o => o.SortOrder).ThenBy(o => o.Label)
                        .Select(o => new ResultOptionDto
                        {
                            Id        = o.Id,
                            Label     = o.Label,
                            Text      = o.Text,
                            IsCorrect = o.IsCorrect
                        })
                        .ToList(),
                    CorrectOptionIds       = correctOptionIds,
                    CorrectNumericalAnswer = q.NumericalAnswer,
                    StudentSelectedOptionIds = studentOptionIds,
                    StudentNumericalValue  = answer?.NumericalValue,
                    IsAttempted            = isAttempted,
                    IsCorrect              = isCorrect,
                    MarksAwarded           = marksAwarded,
                    MaxMarks               = aq.MarksForCorrect,
                    Solution               = q.Solution
                };
            })
            .ToList();

        // ── Section results (from persisted rows) ────────────────────────────
        var sections = attempt.SectionResults
            .OrderBy(r => r.SectionIndex)
            .Select(r => new SectionResultDto
            {
                SectionName    = r.SectionName,
                SectionIndex   = r.SectionIndex,
                TotalQuestions = r.TotalQuestions,
                Attempted      = r.Attempted,
                Correct        = r.Correct,
                Incorrect      = r.Incorrect,
                Unattempted    = r.Unattempted,
                MarksObtained  = r.MarksObtained,
                MaxMarks       = r.MaxMarks
            })
            .ToList();

        return new AttemptResultDto
        {
            AttemptId              = attempt.Id,
            TestTitle              = test.Title,
            StudentName            = student is null ? "" : $"{student.FirstName} {student.LastName}".Trim(),
            StartedAt              = attempt.StartedAt,
            SubmittedAt            = attempt.SubmittedAt ?? DateTime.UtcNow,
            DurationSecondsUsed    = attempt.DurationSecondsUsed,
            TotalMarksObtained     = attempt.TotalMarksObtained ?? 0m,
            TotalMarksPossible     = attempt.TotalMarksPossible ?? 0m,
            Percentage             = attempt.Percentage ?? 0m,
            IsPassed               = attempt.IsPassed ?? false,
            PassingPercent         = test.PassingPercent,
            NegativeMarkingEnabled = test.NegativeMarkingEnabled,
            ViolationCount         = CountViolations(attempt.ViolationLog),
            Sections               = sections,
            Questions              = questionResults
        };
    }

    /// <summary>
    /// Returns the EF IQueryable for candidate Published questions matching a section's criteria.
    /// </summary>
    private IQueryable<Question> BuildCandidateQuery(TestSection section)
    {
        // Include both Draft and Published questions so new tests work immediately.
        // Prefer Published first; Draft questions are included as fallback so the
        // exam can run even before questions are formally published.
        var query = _db.Questions
            .Where(q => q.SubjectId == section.SubjectId);

        if (section.DifficultyLevelId.HasValue)
            query = query.Where(q => q.DifficultyLevelId == section.DifficultyLevelId.Value);

        return query;
    }

    /// <summary>Parses "3,7,12" → [3, 7, 12]. Returns empty list for null/empty input.</summary>
    private static List<int> ParseOptionIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        var result = new List<int>();
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var id))
                result.Add(id);
        }
        return result;
    }

    /// <summary>Counts entries in the ViolationLog JSON array. Returns 0 on parse error or null input.</summary>
    private static int CountViolations(string? violationLog)
    {
        if (string.IsNullOrWhiteSpace(violationLog))
            return 0;

        try
        {
            using var doc = JsonDocument.Parse(violationLog);
            return doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement.GetArrayLength()
                : 0;
        }
        catch
        {
            return 0;
        }
    }
}

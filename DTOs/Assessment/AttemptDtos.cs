using GridAcademy.Data.Entities.Content;
using GridAcademy.Data.Entities.Assessment;

namespace GridAcademy.DTOs.Assessment;

// ── Question/option DTOs for student (no correct answers exposed) ─────────

public class AttemptOptionDto
{
    public int Id { get; set; }
    public char Label { get; set; }
    public string Text { get; set; } = "";
}

public class AttemptQuestionDto
{
    public int AttemptQuestionId { get; set; }   // PK of AttemptQuestion row
    public Guid QuestionId { get; set; }
    public int SectionIndex { get; set; }
    public string SectionName { get; set; } = "";
    public int DisplayOrder { get; set; }         // global 1-based question number
    public int DisplayOrderInSection { get; set; }
    public string Text { get; set; } = "";
    public QuestionType QuestionType { get; set; }
    public List<AttemptOptionDto> Options { get; set; } = [];  // empty for NAT
    public bool IsVisited { get; set; }
    public bool IsMarkedForReview { get; set; }
    public decimal MarksForCorrect { get; set; }
    public decimal NegativeMarks { get; set; }
    // NOTE: IsCorrect is intentionally NOT included — correct answers hidden during exam
}

// ── Attempt lifecycle DTOs ────────────────────────────────────────────────

public class AttemptStartDto
{
    public Guid AttemptId { get; set; }
    public string TestTitle { get; set; } = "";
    public string? Instructions { get; set; }
    public int DurationSeconds { get; set; }     // DurationMinutes × 60
    public int SecondsElapsed { get; set; }      // for resume on page reload
    public int TotalQuestions { get; set; }
    public bool NegativeMarkingEnabled { get; set; }
    public List<AttemptQuestionDto> Questions { get; set; } = [];
    public List<SavedAnswerDto> SavedAnswers { get; set; } = [];  // for resume
}

public class AttemptStateDto : AttemptStartDto
{
    public AttemptStatus Status { get; set; }
}

public class SavedAnswerDto
{
    public Guid QuestionId { get; set; }
    public List<int> SelectedOptionIds { get; set; } = [];
    public decimal? NumericalValue { get; set; }
    public bool IsClear { get; set; }
    public bool IsMarkedForReview { get; set; }
    public bool IsVisited { get; set; }
}

// ── Save answer request (from client JS) ─────────────────────────────────

public class SaveAnswerRequest
{
    public Guid QuestionId { get; set; }
    public List<int> SelectedOptionIds { get; set; } = [];
    public decimal? NumericalValue { get; set; }
    public bool IsClear { get; set; } = false;
}

public class ViolationRequest
{
    public string ViolationType { get; set; } = "";  // tab_switch, copy_paste, fullscreen_exit, context_menu
}

// ── Result DTOs ───────────────────────────────────────────────────────────

public class SectionResultDto
{
    public string SectionName { get; set; } = "";
    public int SectionIndex { get; set; }
    public int TotalQuestions { get; set; }
    public int Attempted { get; set; }
    public int Correct { get; set; }
    public int Incorrect { get; set; }
    public int Unattempted { get; set; }
    public decimal MarksObtained { get; set; }
    public decimal MaxMarks { get; set; }
}

public class QuestionResultDto
{
    public int DisplayOrder { get; set; }
    public string SectionName { get; set; } = "";
    public string QuestionText { get; set; } = "";
    public QuestionType QuestionType { get; set; }
    public List<ResultOptionDto> Options { get; set; } = [];
    public List<int> CorrectOptionIds { get; set; } = [];        // correct answers (revealed at result)
    public decimal? CorrectNumericalAnswer { get; set; }
    public List<int> StudentSelectedOptionIds { get; set; } = [];
    public decimal? StudentNumericalValue { get; set; }
    public bool IsAttempted { get; set; }
    public bool IsCorrect { get; set; }
    public decimal MarksAwarded { get; set; }
    public decimal MaxMarks { get; set; }
    public string? Solution { get; set; }  // shown in result for learning
}

public class ResultOptionDto
{
    public int Id { get; set; }
    public char Label { get; set; }
    public string Text { get; set; } = "";
    public bool IsCorrect { get; set; }  // revealed in result view
}

public class AttemptResultDto
{
    public Guid AttemptId { get; set; }
    public string TestTitle { get; set; } = "";
    public string StudentName { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int DurationSecondsUsed { get; set; }
    public decimal TotalMarksObtained { get; set; }
    public decimal TotalMarksPossible { get; set; }
    public decimal Percentage { get; set; }
    public bool IsPassed { get; set; }
    public decimal PassingPercent { get; set; }
    public bool NegativeMarkingEnabled { get; set; }
    public int ViolationCount { get; set; }
    public List<SectionResultDto> Sections { get; set; } = [];
    public List<QuestionResultDto> Questions { get; set; } = [];
}

// ── Admin attempt summary ─────────────────────────────────────────────────

public class AttemptSummaryDto
{
    public Guid AttemptId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public string StudentEmail { get; set; } = "";
    public int AttemptNumber { get; set; }
    public AttemptStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal? Percentage { get; set; }
    public bool? IsPassed { get; set; }
    public int ViolationCount { get; set; }
}

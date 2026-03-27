using GridAcademy.DTOs.Content.Masters;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Content.Masters;

[Authorize(Roles = "Admin,Instructor")]
public class IndexModel : PageModel
{
    private readonly IMasterService _svc;
    public IndexModel(IMasterService svc) => _svc = svc;

    // ── Display data ──────────────────────────────────────────────────────────
    public List<QuestionTypeDto>    QuestionTypes    { get; set; } = [];
    public List<SubjectDto>         Subjects         { get; set; } = [];
    public List<TopicDto>           Topics           { get; set; } = [];
    public List<DifficultyLevelDto> DifficultyLevels { get; set; } = [];
    public List<ComplexityLevelDto> ComplexityLevels { get; set; } = [];
    public List<ExamTypeDto>        ExamTypes        { get; set; } = [];
    public List<MarksDto>           Marks            { get; set; } = [];
    public List<NegativeMarksDto>   NegativeMarks    { get; set; } = [];
    public List<TagDto>             Tags             { get; set; } = [];

    [TempData] public string? ActiveTab { get; set; }

    // ── Bound form models (classes so model-binding works) ────────────────────
    public class AddForm            { public string Name { get; set; } = ""; public bool IsActive { get; set; } = true; public int SortOrder { get; set; } = 0; }
    public class AddTopicForm       { public string Name { get; set; } = ""; public int SubjectId { get; set; } = 0;    public bool IsActive { get; set; } = true; public int SortOrder { get; set; } = 0; }
    public class AddMarksForm       { public string Name { get; set; } = ""; public decimal Value { get; set; } = 0;    public bool IsActive { get; set; } = true; public int SortOrder { get; set; } = 0; }
    public class EditQTypeForm      { public string Name { get; set; } = ""; public string? Description { get; set; } = ""; public bool IsActive { get; set; } = true; }

    [BindProperty] public AddForm      NewSubject      { get; set; } = new();
    [BindProperty] public AddTopicForm NewTopic        { get; set; } = new();
    [BindProperty] public AddForm      NewDifficulty   { get; set; } = new();
    [BindProperty] public AddForm      NewComplexity   { get; set; } = new();
    [BindProperty] public AddForm      NewExamType     { get; set; } = new();
    [BindProperty] public AddForm      NewTag          { get; set; } = new();
    [BindProperty] public AddMarksForm NewMarks        { get; set; } = new();
    [BindProperty] public AddMarksForm NewNegativeMarks{ get; set; } = new();
    [BindProperty] public EditQTypeForm EditQType      { get; set; } = new();

    // ── GET ───────────────────────────────────────────────────────────────────
    public async Task OnGetAsync() => await LoadAllAsync();

    // ── SUBJECTS ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAddSubjectAsync()
    {
        if (string.IsNullOrWhiteSpace(NewSubject.Name))
            return Redirect("subjects", "Subject name is required.");
        await _svc.CreateSubjectAsync(new(NewSubject.Name, NewSubject.IsActive, NewSubject.SortOrder));
        return Redirect("subjects", null, $"Subject '{NewSubject.Name}' added.");
    }
    public async Task<IActionResult> OnPostDeleteSubjectAsync(int id)
    {
        await _svc.DeleteSubjectAsync(id);
        return Redirect("subjects", null, "Subject deleted.");
    }

    // ── TOPICS ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAddTopicAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTopic.Name))
            return Redirect("topics", "Topic name is required.");
        if (NewTopic.SubjectId == 0)
            return Redirect("topics", "Please select a subject.");
        await _svc.CreateTopicAsync(new(NewTopic.Name, NewTopic.SubjectId, NewTopic.IsActive, NewTopic.SortOrder));
        return Redirect("topics", null, $"Topic '{NewTopic.Name}' added.");
    }
    public async Task<IActionResult> OnPostDeleteTopicAsync(int id)
    {
        await _svc.DeleteTopicAsync(id);
        return Redirect("topics", null, "Topic deleted.");
    }

    // ── DIFFICULTY LEVELS ─────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAddDifficultyAsync()
    {
        if (string.IsNullOrWhiteSpace(NewDifficulty.Name))
            return Redirect("difficulty", "Name is required.");
        await _svc.CreateDifficultyLevelAsync(new(NewDifficulty.Name, NewDifficulty.IsActive, NewDifficulty.SortOrder));
        return Redirect("difficulty", null, $"Difficulty '{NewDifficulty.Name}' added.");
    }
    public async Task<IActionResult> OnPostDeleteDifficultyAsync(int id)
    {
        await _svc.DeleteDifficultyLevelAsync(id);
        return Redirect("difficulty", null, "Difficulty level deleted.");
    }

    // ── COMPLEXITY LEVELS ─────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAddComplexityAsync()
    {
        if (string.IsNullOrWhiteSpace(NewComplexity.Name))
            return Redirect("complexity", "Name is required.");
        await _svc.CreateComplexityLevelAsync(new(NewComplexity.Name, NewComplexity.IsActive, NewComplexity.SortOrder));
        return Redirect("complexity", null, $"Complexity '{NewComplexity.Name}' added.");
    }
    public async Task<IActionResult> OnPostDeleteComplexityAsync(int id)
    {
        await _svc.DeleteComplexityLevelAsync(id);
        return Redirect("complexity", null, "Complexity level deleted.");
    }

    // ── EXAM TYPES ────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAddExamTypeAsync()
    {
        if (string.IsNullOrWhiteSpace(NewExamType.Name))
            return Redirect("examtypes", "Name is required.");
        await _svc.CreateExamTypeAsync(new(NewExamType.Name, NewExamType.IsActive, NewExamType.SortOrder));
        return Redirect("examtypes", null, $"Exam Type '{NewExamType.Name}' added.");
    }
    public async Task<IActionResult> OnPostDeleteExamTypeAsync(int id)
    {
        await _svc.DeleteExamTypeAsync(id);
        return Redirect("examtypes", null, "Exam type deleted.");
    }

    // ── TAGS ─────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAddTagAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTag.Name))
            return Redirect("tags", "Tag name is required.");
        await _svc.CreateTagAsync(new(NewTag.Name, NewTag.IsActive, NewTag.SortOrder));
        return Redirect("tags", null, $"Tag '{NewTag.Name}' added.");
    }
    public async Task<IActionResult> OnPostDeleteTagAsync(int id)
    {
        await _svc.DeleteTagAsync(id);
        return Redirect("tags", null, "Tag deleted.");
    }

    // ── MARKS ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAddMarksAsync()
    {
        if (string.IsNullOrWhiteSpace(NewMarks.Name))
            return Redirect("marks", "Name is required.");
        await _svc.CreateMarksAsync(new(NewMarks.Name, NewMarks.Value, NewMarks.IsActive, NewMarks.SortOrder));
        return Redirect("marks", null, $"Marks '{NewMarks.Name}' added.");
    }
    public async Task<IActionResult> OnPostDeleteMarksAsync(int id)
    {
        await _svc.DeleteMarksAsync(id);
        return Redirect("marks", null, "Marks entry deleted.");
    }

    // ── NEGATIVE MARKS ────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAddNegativeMarksAsync()
    {
        if (string.IsNullOrWhiteSpace(NewNegativeMarks.Name))
            return Redirect("negmarks", "Name is required.");
        await _svc.CreateNegativeMarksAsync(new(NewNegativeMarks.Name, NewNegativeMarks.Value, NewNegativeMarks.IsActive, NewNegativeMarks.SortOrder));
        return Redirect("negmarks", null, $"Negative Marks '{NewNegativeMarks.Name}' added.");
    }
    public async Task<IActionResult> OnPostDeleteNegativeMarksAsync(int id)
    {
        await _svc.DeleteNegativeMarksAsync(id);
        return Redirect("negmarks", null, "Negative marks entry deleted.");
    }

    // ── QUESTION TYPES (edit name/description/active only — no add/delete) ───
    public async Task<IActionResult> OnPostUpdateQuestionTypeAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(EditQType.Name))
            return Redirect("qtypes", "Question type name is required.");
        try
        {
            await _svc.UpdateQuestionTypeAsync(id, EditQType.Name, EditQType.Description, EditQType.IsActive);
            return Redirect("qtypes", null, "Question type updated.");
        }
        catch (KeyNotFoundException ex)
        {
            return Redirect("qtypes", ex.Message);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private IActionResult Redirect(string tab, string? error = null, string? success = null)
    {
        ActiveTab = tab;
        if (error   != null) TempData["Error"]   = error;
        if (success != null) TempData["Success"]  = success;
        return RedirectToPage();
    }

    private async Task LoadAllAsync()
    {
        QuestionTypes    = await _svc.GetQuestionTypesAsync(activeOnly: false);
        Subjects         = await _svc.GetSubjectsAsync(activeOnly: false);
        Topics           = await _svc.GetTopicsAsync(activeOnly: false);
        DifficultyLevels = await _svc.GetDifficultyLevelsAsync();
        ComplexityLevels = await _svc.GetComplexityLevelsAsync();
        ExamTypes        = await _svc.GetExamTypesAsync();
        Marks            = await _svc.GetMarksAsync();
        NegativeMarks    = await _svc.GetNegativeMarksAsync();
        Tags             = await _svc.GetTagsAsync();
    }
}

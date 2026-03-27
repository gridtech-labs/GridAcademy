using GridAcademy.Common;
using GridAcademy.Data.Entities.Content;
using GridAcademy.DTOs.Content.Masters;
using GridAcademy.DTOs.Content.Questions;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Content.Questions;

[Authorize(Roles = "Admin,Instructor")]
public class IndexModel : PageModel
{
    private readonly IQuestionService _questions;
    private readonly IMasterService   _masters;

    public IndexModel(IQuestionService questions, IMasterService masters)
    {
        _questions = questions;
        _masters   = masters;
    }

    // ── Filter (bound from GET query string) ──────────────────────────────────
    [BindProperty(SupportsGet = true)] public string?         Search          { get; set; }
    [BindProperty(SupportsGet = true)] public int?            SubjectId       { get; set; }
    [BindProperty(SupportsGet = true)] public int?            TopicId         { get; set; }
    [BindProperty(SupportsGet = true)] public int?            DifficultyLevelId { get; set; }
    [BindProperty(SupportsGet = true)] public int?            ExamTypeId      { get; set; }
    [BindProperty(SupportsGet = true)] public QuestionType?   QuestionType    { get; set; }
    [BindProperty(SupportsGet = true)] public QuestionStatus? Status          { get; set; }
    [BindProperty(SupportsGet = true)] public int             CurrentPage     { get; set; } = 1;
    public const int PageSize = 20;

    // ── Results ───────────────────────────────────────────────────────────────
    public PagedResult<QuestionDto>  Questions        { get; set; } = new();
    public List<SubjectDto>          Subjects         { get; set; } = [];
    public List<TopicDto>            Topics           { get; set; } = [];
    public List<DifficultyLevelDto>  DifficultyLevels { get; set; } = [];
    public List<ExamTypeDto>         ExamTypes        { get; set; } = [];

    public async Task OnGetAsync()
    {
        var filter = new QuestionListRequest
        {
            Search            = Search,
            SubjectId         = SubjectId,
            TopicId           = TopicId,
            DifficultyLevelId = DifficultyLevelId,
            ExamTypeId        = ExamTypeId,
            QuestionType      = QuestionType,
            Status            = Status,
            Page              = CurrentPage,
            PageSize          = PageSize
        };

        Questions        = await _questions.GetQuestionsAsync(filter);
        Subjects         = await _masters.GetSubjectsAsync();
        Topics           = SubjectId.HasValue
                           ? await _masters.GetTopicsAsync(SubjectId)
                           : await _masters.GetTopicsAsync();
        DifficultyLevels = await _masters.GetDifficultyLevelsAsync();
        ExamTypes        = await _masters.GetExamTypesAsync();
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id)
    {
        try
        {
            await _questions.PublishAsync(id);
            TempData["Success"] = "Question published.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not publish question: {ex.Message}";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnpublishAsync(Guid id)
    {
        try
        {
            await _questions.UnpublishAsync(id);
            TempData["Success"] = "Question moved back to draft.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not unpublish question: {ex.Message}";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _questions.DeleteAsync(id);
            TempData["Success"] = "Question deleted.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not delete question: {ex.Message}";
        }
        return RedirectToPage();
    }
}
